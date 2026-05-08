namespace ETL

open System
open System.Text.Json

module Parser =

    let parseJson (jsonData: string) : DailyQuote list =
        try
            use doc = JsonDocument.Parse(jsonData)
            let root = doc.RootElement
            let chart = root.GetProperty("chart")
            
            if chart.GetProperty("error").ValueKind <> JsonValueKind.Null then
                []
            else
                let result = chart.GetProperty("result").[0]
                
                let timestamps = result.GetProperty("timestamp").EnumerateArray() |> Seq.toArray
                
                // Pula toda a bagagem de OHLC e pega direto o vetor numérico do fechamento
                let quote = result.GetProperty("indicators").GetProperty("quote").[0]
                let closes = quote.GetProperty("close").EnumerateArray() |> Seq.toArray
                
                [| 0 .. timestamps.Length - 1 |]
                |> Array.choose (fun i ->
                    if closes.[i].ValueKind = JsonValueKind.Null then 
                        None
                    else
                        let date = DateTimeOffset.FromUnixTimeSeconds(timestamps.[i].GetInt64()).DateTime.Date
                        Some {
                            Date = date
                            AdjClose = closes.[i].GetDouble()
                        }
                )
                |> Array.toList
        with
        | ex -> 
            printfn "⚠️ Falha crítica no parsing do JSON: %s" ex.Message
            []