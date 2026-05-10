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
    // Usamos fold2 (zero-alocação) no lugar do map2 para não sobrecarregar o Garbage Collector
    Array.fold2 (fun acc a b -> acc + (a * b)) 0.0 v1 v2

// ==========================================
// Preparação de Matrizes (Rodam 1x por combinação)
// ==========================================

/// <summary>
/// Calcula o retorno médio diário para cada ativo a partir da matriz histórica de retornos.
/// </summary>
let calculateMeanReturns (returnsMatrix: float[,]) : float array =
    let numDays = Array2D.length1 returnsMatrix
    let numAssets = Array2D.length2 returnsMatrix
    Array.init numAssets (fun j ->
        Array.init numDays (fun i -> returnsMatrix.[i, j])
        |> Array.average
    )

/// <summary>
/// Constrói a Matriz de Covariância a partir da matriz de retornos e das médias.
/// Usa o denominador amostral (N-1).
/// </summary>
let calculateCovarianceMatrix (returnsMatrix: float[,]) (means: float array) : float[,] =
    let numDays = Array2D.length1 returnsMatrix
    let numAssets = Array2D.length2 returnsMatrix
    let nFloat = float (numDays - 1)
    
    Array2D.init numAssets numAssets (fun i j ->
        Array.init numDays (fun d ->
            (returnsMatrix.[d, i] - means.[i]) * (returnsMatrix.[d, j] - means.[j])
        )
        |> Array.sum
        |> fun s -> s / nFloat
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
let calculateVolatility (weights: float array) (covMatrix: float[,]) : float =
    let n = weights.Length
    let variance =
        Seq.allPairs [0 .. n - 1] [0 .. n - 1]
        |> Seq.sumBy (fun (i, j) -> weights.[i] * weights.[j] * covMatrix.[i, j])
    
    (sqrt variance) * (sqrt TradingDays)

/// <summary>
/// Calcula o Sharpe Ratio anualizado.
/// Fórmula: SR = (μ - r_free) / σ
/// </summary>
let calculateSharpe (expectedReturn: float) (volatility: float) (riskFreeRate: float) : float =
    if volatility <= 0.0 then 0.0 
    else (expectedReturn - riskFreeRate) / volatility