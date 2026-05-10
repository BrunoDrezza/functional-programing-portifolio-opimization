module PortfolioEngine.Simulator

open System
open PortfolioEngine.Domain
open PortfolioEngine.MathEngine

// ==========================================
// Lógica Híbrida de Geração de Pesos
// ==========================================

/// <summary>
/// Algoritmo puro de redistribuição (Water-filling).
/// Corta os pesos que excedem o limite e redistribui o excesso equitativamente.
/// </summary>
let rec private redistributeWeights (weights: float array) (maxLimit: float) : float array =
    let capped = weights |> Array.map (fun w -> min w maxLimit)
    let excess = Array.map2 (fun orig cap -> max 0.0 (orig - cap)) weights capped |> Array.sum
    let validCount = capped |> Array.filter (fun w -> w < maxLimit) |> Array.length

    if excess <= 1e-9 || validCount = 0 then
        capped
    else
        let distribution = excess / float validCount
        let distributed = capped |> Array.map (fun w -> if w < maxLimit then w + distribution else w)
        let needsMore = distributed |> Array.exists (fun w -> w > maxLimit + 1e-9)
        if needsMore then redistributeWeights distributed maxLimit
        else distributed

/// <summary>
/// Gera um vetor de pesos normalizado. Utiliza a estratégia de "Fast Path".
/// Se o sorteio natural já respeitar os 20%, avança direto. Se não, invoca o "Slow Path" (redistribuição).
/// </summary>
let generateValidWeights (numAssets: int) : Weights =
    // Random.Shared é nativo e thread-safe no .NET moderno, essencial para paralelismo seguro
    let rand = Random.Shared
    let raw = Array.init numAssets (fun _ -> rand.NextDouble())
    
    // Normalização (Soma = 1)
    let sum = Array.sum raw
    let normalized = raw |> Array.map (fun x -> x / sum)
    
    let maxW = Array.max normalized
    if maxW <= 0.20 then
        normalized // Fast Path (ocorre na esmagadora maioria das vezes para 25+ ativos)
    else
        redistributeWeights normalized 0.20 // Slow Path (corrige sem desperdiçar a simulação)

// ==========================================
// Motor Estocástico
// ==========================================

/// <summary>
/// Executa as N simulações de Monte Carlo para um subconjunto de ativos.
/// Encontra a carteira na Fronteira Eficiente (Maior Sharpe Ratio).
/// </summary>
let simulateMonteCarlo (subset: AssetSubset) (numSims: int) (riskFreeRate: float) : PortfolioResult =
    let numAssets = subset.Assets.Length
    
    Seq.init numSims (fun _ ->
        let w = generateValidWeights numAssets
        let ret = calculatePortfolioReturn w subset.MeanReturns
        let vol = calculateVolatility w subset.Covariance
        { Weights = w
          ExpectedReturn = ret
          Volatility = vol
          SharpeRatio = calculateSharpe ret vol riskFreeRate }
    )
    |> Seq.maxBy (fun p -> p.SharpeRatio)

/// <summary>
/// Executa Monte Carlo gerando as coordenadas para plotagem da Fronteira Eficiente.
/// Usada apenas após a descoberta da carteira vencedora.
/// </summary>
let generatePlotData (subset: AssetSubset) (numSims: int) (riskFreeRate: float) : (float * float * float) array =
    let numAssets = subset.Assets.Length
    
    Array.init numSims (fun _ ->
        let w = generateValidWeights numAssets
        let ret = calculatePortfolioReturn w subset.MeanReturns
        let vol = calculateVolatility w subset.Covariance
        (vol, ret, calculateSharpe ret vol riskFreeRate)
    )