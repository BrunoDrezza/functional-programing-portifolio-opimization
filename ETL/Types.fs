namespace ETL

open System

type DailyQuote = {
    Date: DateTime
    AdjClose: float
}

type EtlError =
    | NetworkError of string
    | ParsingError of string
    | InvalidDataError of string