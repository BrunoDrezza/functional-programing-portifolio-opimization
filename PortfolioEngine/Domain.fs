module PortfolioEngine.Domain

// Aliases para facilitar a leitura matemática e garantir performance
type AssetName = string
type Weights = float array
type MeanReturns = float array
type CovarianceMatrix = float[,] // Matriz 2D nativa do .NET

// Record imutável que representa o resultado final de uma carteira avaliada
type PortfolioResult = {
    Assets: AssetName array
    Weights: Weights
    ExpectedReturn: float
    Volatility: float
    SharpeRatio: float
}