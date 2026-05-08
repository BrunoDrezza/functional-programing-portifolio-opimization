namespace ETL

open System
open System.IO
open System.Globalization
open System.Collections.Generic

module Transform =

    // Mantemos a função de salvar individual (para cache local)
    let saveToCsv (symbol: string) (quotes: DailyQuote list) : Result<string, EtlError> =
        let filePath = sprintf "data/data_raw/%s_returns.csv" symbol
        try
            let dir = Path.GetDirectoryName(filePath)
            if not (Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
            use writer = new StreamWriter(filePath)
            writer.WriteLine("Date,AdjClose")
            quotes |> List.iter (fun q -> 
                writer.WriteLine(sprintf "%s,%s" (q.Date.ToString("yyyy-MM-dd")) (q.AdjClose.ToString(CultureInfo.InvariantCulture)))
            )
            Ok filePath
        with | ex -> Error (InvalidDataError ex.Message)

    /// <summary>
    /// Consolida todos os CSVs individuais em uma única matriz (Date, TICKER1, TICKER2...).
    /// </summary>
    let consolidateToMatrix (tickers: string array) : Result<string, EtlError> =
        let outputPath = "data/data_raw/all_returns.csv"
        try
            // Estrutura para alinhar: Data -> (Ticker -> Preço)
            let matrix = SortedDictionary<DateTime, Dictionary<string, float>>()

            for symbol in tickers do
                let filePath = sprintf "data/data_raw/%s_returns.csv" symbol
                if File.Exists(filePath) then
                    let lines = File.ReadAllLines(filePath) |> Array.skip 1 // Pula cabeçalho
                    for line in lines do
                        let parts = line.Split(',')
                        let date = DateTime.Parse(parts.[0])
                        let price = Double.Parse(parts.[1], CultureInfo.InvariantCulture)
                        
                        if not (matrix.ContainsKey(date)) then
                            matrix.[date] <- Dictionary<string, float>()
                        matrix.[date].[symbol] <- price

            // Escreve o arquivo consolidado
            use writer = new StreamWriter(outputPath)
            
            // Cabeçalho: Date,AAPL,AMGN...
            let header = "Date," + (String.Join(",", tickers))
            writer.WriteLine(header)

            // Linhas: Data, Preço1, Preço2...
            for kvp in matrix do
                let dateStr = kvp.Key.ToString("yyyy-MM-dd")
                let prices = 
                    tickers 
                    |> Array.map (fun t -> 
                        if kvp.Value.ContainsKey(t) then 
                            kvp.Value.[t].ToString(CultureInfo.InvariantCulture)
                        else "" // Caso falte dado, fica vazio (será tratado no MathEngine)
                    )
                writer.WriteLine(dateStr + "," + (String.Join(",", prices)))

            Ok outputPath
        with | ex -> Error (InvalidDataError ("Falha na consolidação: " + ex.Message))