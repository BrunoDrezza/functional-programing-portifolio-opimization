module DataLoader

open System
open System.IO
open System.Globalization

/// <summary>
/// Lê o arquivo CSV consolidado e converte a série de preços em uma Matriz de Retornos 2D.
/// Aplica tratamento quantitativo para eventuais dados faltantes.
/// </summary>
let loadReturnsMatrix (filePath: string) : string array * float[,] =
    let lines = File.ReadAllLines(filePath)
    
    let header = lines.[0].Split(',')
    let assetNames = header.[1..] 
    let numAssets = assetNames.Length
    let numDays = lines.Length - 1
    
    let prices = Array2D.zeroCreate<float> numDays numAssets
    
    for i in 1 .. lines.Length - 1 do
        let parts = lines.[i].Split(',')
        let dayIndex = i - 1
        for j in 0 .. numAssets - 1 do
            let priceStr = parts.[j + 1]
            
            if String.IsNullOrWhiteSpace(priceStr) then
                prices.[dayIndex, j] <- 
                    if dayIndex > 0 then prices.[dayIndex - 1, j] 
                    else 1.0 
            else
                prices.[dayIndex, j] <- Double.Parse(priceStr, CultureInfo.InvariantCulture)
                
    let numReturns = numDays - 1
    let returnsMatrix = Array2D.zeroCreate<float> numReturns numAssets
    
    for i in 0 .. numReturns - 1 do
        for j in 0 .. numAssets - 1 do
            let prevPrice = prices.[i, j]
            let currentPrice = prices.[i + 1, j]
            returnsMatrix.[i, j] <- (currentPrice / prevPrice) - 1.0
            
    (assetNames, returnsMatrix)