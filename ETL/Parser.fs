namespace ETL

open System
open System.Text.Json

module Parser =

    let parseJson (jsonData: string) : Result<DailyQuote list, EtlError> =
        try
            use doc = JsonDocument.Parse(jsonData)
            let root = doc.RootElement
            let chart = root.GetProperty("chart")
            
            if chart.GetProperty("error").ValueKind <> JsonValueKind.Null then
                Error (InvalidDataError "A API do Yahoo retornou um erro estrutural.")
            else
                let result = chart.GetProperty("result").[0]
                let timestamps = result.GetProperty("timestamp").EnumerateArray() |> Seq.toArray
                
                // Pula toda a bagagem de OHLC e pega direto o vetor numérico do fechamento
                let quote = result.GetProperty("indicators").GetProperty("quote").[0]
                let closes = quote.GetProperty("close").EnumerateArray() |> Seq.toArray
                
                let quotes = 
                    [| 0 .. timestamps.Length - 1 |]
                    |> Array.choose (fun i ->
                        if closes.[i].ValueKind = JsonValueKind.Null then 
                            None
                        else
                            let date = DateTimeOffset.FromUnixTimeSeconds(timestamps.[i].GetInt64()).DateTime.Date
                            Some { Date = date; AdjClose = closes.[i].GetDouble() }
                    )
                    |> Array.toList
                
                Ok quotes // Retorna Sucesso Embrulhado na Monad Result
        with
        | ex -> 
            Error (ParsingError ex.Message)