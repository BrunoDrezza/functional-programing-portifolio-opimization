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
    let n = weights.Length
    let mutable excess = 0.0
    let mutable validCount = 0

    // Identifica excessos e conta quantos ativos ainda podem receber peso
    for i in 0 .. n - 1 do
        if weights.[i] > maxLimit then
            excess <- excess + (weights.[i] - maxLimit)
            weights.[i] <- maxLimit
        else if weights.[i] < maxLimit then
            validCount <- validCount + 1

    // Se o excesso for residual (erro de precisão de floating point) ou não houver a quem dar, termina
    if excess <= 1e-9 || validCount = 0 then
        weights
    else
        let distribution = excess / float validCount
        let newWeights = Array.copy weights
        
        for i in 0 .. n - 1 do
            if newWeights.[i] < maxLimit then
                newWeights.[i] <- newWeights.[i] + distribution
        
        // A redistribuição pode ter empurrado um ativo para lá do limite. Recursão se necessário.
        let needsMore = newWeights |> Array.exists (fun w -> w > maxLimit + 1e-9)
        if needsMore then redistributeWeights newWeights maxLimit
        else newWeights

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
    
    // Controlo de estado estritamente local (Impede a alocação de 1 milhão de objetos na Heap)
    let mutable bestSharpe = -Double.MaxValue
    let mutable bestReturn = 0.0
    let mutable bestVol = 0.0
    let mutable bestWeights = Array.empty<float>

    for _ in 1 .. numSims do
        let w = generateValidWeights numAssets
        
        let ret = calculatePortfolioReturn w subset.MeanReturns
        let vol = calculateVolatility w subset.Covariance
        let sharpe = calculateSharpe ret vol riskFreeRate
        
        // Mantém em memória apenas o vetor supremo
        if sharpe > bestSharpe then
            bestSharpe <- sharpe
            bestReturn <- ret
            bestVol <- vol
            bestWeights <- w

    // Devolve um Record imutável respeitando o paradigma funcional
    {
        Weights = bestWeights
        ExpectedReturn = bestReturn
        Volatility = bestVol
        SharpeRatio = bestSharpe
    }

/// <summary>
/// Executa Monte Carlo gerando as coordenadas para plotagem da Fronteira Eficiente.
/// Usada apenas após a descoberta da carteira vencedora.
/// </summary>
let generatePlotData (subset: AssetSubset) (numSims: int) (riskFreeRate: float) =
    let numAssets = subset.Assets.Length
    let plotPoints = Array.zeroCreate<(float * float * float)> numSims

    for i in 0 .. numSims - 1 do
        let w = generateValidWeights numAssets
        let ret = calculatePortfolioReturn w subset.MeanReturns
        let vol = calculateVolatility w subset.Covariance
        let sharpe = calculateSharpe ret vol riskFreeRate
        
        plotPoints.[i] <- (vol, ret, sharpe)
        
    plotPoints