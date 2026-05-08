namespace ETL

open System
open System.Net.Http
open System.Threading.Tasks

module Fetcher =

    let private client = 
        let c = new HttpClient()
        c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0")
        c

    let private toUnixTime (date: DateTime) : int64 =
        let offset = DateTimeOffset(date, TimeSpan.Zero)
        offset.ToUnixTimeSeconds()

    let private buildUrl (symbol: string) (startDate: DateTime) (endDate: DateTime) : string =
        let period1 = toUnixTime startDate
        let period2 = toUnixTime endDate
        // A rota dos quantitativos: Retorna JSON puro com arrays de OHLCV, imune a bloqueios de CSV.
        sprintf "https://query2.finance.yahoo.com/v8/finance/chart/%s?period1=%d&period2=%d&interval=1d" symbol period1 period2

    let fetchYahooDataAsync (symbol: string) (startDate: DateTime) (endDate: DateTime) : Async<Result<string, EtlError>> =
        async {
            let url = buildUrl symbol startDate endDate
            try
                let! response = client.GetAsync(url) |> Async.AwaitTask
                
                if response.IsSuccessStatusCode then
                    let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    return Ok content
                else
                    let msg = sprintf "Erro HTTP %d ao buscar %s" (int response.StatusCode) symbol
                    return Error (NetworkError msg)
            with
            | ex -> return Error (NetworkError ex.Message)
        }