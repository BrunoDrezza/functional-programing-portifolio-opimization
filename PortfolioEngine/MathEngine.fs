module PortfolioEngine.MathEngine

open System
open PortfolioEngine.Domain

// ==========================================
// Constantes Financeiras
// ==========================================
let TradingDays = 252.0

// ==========================================
// Funções Auxiliares (Puras)
// ==========================================

/// <summary>
/// Calcula o produto escalar (dot product) entre dois vetores.
/// </summary>
let private dotProduct (v1: float array) (v2: float array) : float =
    let mutable sum = 0.0
    for i in 0 .. v1.Length - 1 do
        sum <- sum + (v1.[i] * v2.[i])
    sum

// ==========================================
// Preparação de Matrizes (Rodam 1x por combinação)
// ==========================================

/// <summary>
/// Calcula o retorno médio diário para cada ativo a partir da matriz histórica de retornos.
/// </summary>
let calculateMeanReturns (returnsMatrix: float[,]) : ExpectedReturns =
    let numDays = Array2D.length1 returnsMatrix
    let numAssets = Array2D.length2 returnsMatrix
    
    // Array.init é uma forma puramente funcional de inicializar um array
    Array.init numAssets (fun j ->
        let mutable sum = 0.0
        for i in 0 .. numDays - 1 do
            sum <- sum + returnsMatrix.[i, j]
        sum / float numDays
    )

/// <summary>
/// Constrói a Matriz de Covariância a partir da matriz de retornos e das médias.
/// Usa o denominador amostral (N-1).
/// </summary>
let calculateCovarianceMatrix (returnsMatrix: float[,]) (means: ExpectedReturns) : CovarianceMatrix =
    let numDays = Array2D.length1 returnsMatrix
    let numAssets = Array2D.length2 returnsMatrix
    let nFloat = float (numDays - 1) 
    
    // Array2D.init constrói a matriz imutável no final da execução
    Array2D.init numAssets numAssets (fun i j ->
        let mutable sum = 0.0
        for d in 0 .. numDays - 1 do
            sum <- sum + (returnsMatrix.[d, i] - means.[i]) * (returnsMatrix.[d, j] - means.[j])
        sum / nFloat
    )

// ==========================================
// Motor do Monte Carlo (Roda 1.000.000x por combinação)
// ==========================================

/// <summary>
/// Calcula o retorno esperado anualizado da carteira (μ).
/// Fórmula: r_p = r * w
/// </summary>
let calculatePortfolioReturn (weights: Weights) (expectedDailyReturns: ExpectedReturns) : float =
    let dailyReturn = dotProduct weights expectedDailyReturns
    dailyReturn * TradingDays

/// <summary>
/// Calcula a volatilidade anualizada da carteira (σ).
/// Fórmula: σ_p = sqrt(w^T * C * w) multiplicada pela raiz de 252.
/// </summary>
let calculateVolatility (weights: Weights) (covMatrix: CovarianceMatrix) : float =
    let n = weights.Length
    let mutable variance = 0.0
    
    // Forma quadrática otimizada para evitar alocação de memória no loop
    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            variance <- variance + (weights.[i] * weights.[j] * covMatrix.[i, j])
            
    let dailyVol = sqrt variance
    dailyVol * (sqrt TradingDays)

/// <summary>
/// Calcula o Sharpe Ratio anualizado.
/// Fórmula: SR = (μ - r_free) / σ
/// </summary>
let calculateSharpe (expectedReturn: float) (volatility: float) (riskFreeRate: float) : float =
    if volatility <= 0.0 then 0.0 
    else (expectedReturn - riskFreeRate) / volatility