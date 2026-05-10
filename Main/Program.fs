open System
open System.IO
open System.Diagnostics
open System.Globalization
open PortfolioEngine.Domain
open PortfolioEngine.MathEngine
open PortfolioEngine.Simulator
open DataLoader

// ==========================================
// Módulo de Combinatória (Otimizado com Pruning)
// ==========================================
let getCombinations k (lst: 'a list) =
    let rec comb k n l =
        if k = 0 then [[]]
        elif k > n then [] // A TRAVA DE OURO: Aborta se faltam elementos!
        else
            match l with
            | [] -> []
            | x::xs -> 
                let withX = comb (k - 1) (n - 1) xs |> List.map (fun ys -> x :: ys)
                let withoutX = comb k (n - 1) xs
                withX @ withoutX
    comb k (List.length lst) lst

let generateAllIndicesCombinations (totalAssets: int) (minSize: int) =
    let allIndices = [0 .. totalAssets - 1]
    [| for k in minSize .. totalAssets do
        yield! getCombinations k allIndices |> List.map List.toArray |]

// ==========================================
// Construtor de Subconjuntos
// ==========================================
let createAssetSubset (indices: int array) (allNames: string array) (fullReturns: float[,]) =
    let numDays = Array2D.length1 fullReturns
    let k = indices.Length
    
    let subsetNames = indices |> Array.map (fun i -> allNames.[i])
    
    // Matriz inicializada funcionalmente em uma linha:
    let subsetReturns = Array2D.init numDays k (fun d j -> fullReturns.[d, indices.[j]])
            
    let means = calculateMeanReturns subsetReturns
    let cov = calculateCovarianceMatrix subsetReturns means
    
    { Assets = subsetNames; MeanReturns = means; Covariance = cov }

[<EntryPoint>]
let main argv =
    printfn "=================================================="
    printfn "  Motor de Otimização Combinatória (Dow Jones)    "
    printfn "==================================================\n"

    let dataPath = "data/data_raw/all_returns.csv"
    let assetNames, returnsMatrix = loadReturnsMatrix dataPath
    let totalAssets = assetNames.Length
    
    // Configuração de Teste Rápido
    let minAssetsPerPortfolio = 25
    let simulationsPerCombination = 10_000
    let riskFreeRate = 0.03 
    
    let combinations = generateAllIndicesCombinations totalAssets minAssetsPerPortfolio
    printfn "[1/3] Disparando Paralelismo para %s combinações...\n" (combinations.Length.ToString("N0"))
    
    let sw = Stopwatch.StartNew()

    // FASE 1: Map-Reduce Paralelo (100% Puro Funcional - Sem side-effects)
    let results =
        combinations
        |> Array.Parallel.map (fun indices ->
            let subset = createAssetSubset indices assetNames returnsMatrix
            let bestPort = simulateMonteCarlo subset simulationsPerCombination riskFreeRate
            (indices, subset, bestPort)
        )

    sw.Stop()
    
    printfn "  -> Map-Reduce concluído! %d combinações processadas com sucesso." results.Length
    
    // Encontrar a Campeã Absoluta
    let bestIndices, championSubset, championPortfolio = 
        results |> Array.maxBy (fun (_, _, port) -> port.SharpeRatio)

    // FASE 2: Geração do Gráfico (Apenas para a combinação campeã)
    printfn "\n[2/3] Campeã encontrada! Gerando nuvem de pontos para a Fronteira Eficiente..."
    let plotSimulations = 100_000
    let plotData = generatePlotData championSubset plotSimulations riskFreeRate
    
    let plotPath = "data/efficient_frontier.csv"
    use writer = new StreamWriter(plotPath)
    writer.WriteLine("Volatility,Return,Sharpe")
    for (v, r, s) in plotData do
        writer.WriteLine(sprintf "%s,%s,%s" 
            (v.ToString(CultureInfo.InvariantCulture)) 
            (r.ToString(CultureInfo.InvariantCulture)) 
            (s.ToString(CultureInfo.InvariantCulture)))

    printfn "[3/3] Dados exportados com sucesso para %s\n" plotPath

    // Apresentação
    printfn "=================================================="
    printfn "              A CARTEIRA VENCEDORA                "
    printfn "=================================================="
    printfn "Tempo de Processamento : %.2f segundos" sw.Elapsed.TotalSeconds
    printfn "Sharpe Ratio      : %.4f" championPortfolio.SharpeRatio
    printfn "Retorno Esperado  : %.2f%%" (championPortfolio.ExpectedReturn * 100.0)
    printfn "Volatilidade      : %.2f%%" (championPortfolio.Volatility * 100.0)
    printfn "--------------------------------------------------"
    printfn "Alocação de Pesos Ótima (Restrição Máx 20%%):"
    
    for i in 0 .. championSubset.Assets.Length - 1 do
        let weight = championPortfolio.Weights.[i] * 100.0
        if weight > 0.01 then
            printfn "  %s : %.2f%%" (championSubset.Assets.[i].PadRight(5)) weight
            
    printfn "==================================================\n"

    0