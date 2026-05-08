module PortfolioEngine.Domain

/// <summary>
/// Aliases de tipos quantitativos para clareza semântica e performance.
/// O uso de arrays primitivos (float array) garante contiguidade em memória,
/// otimizando o uso do Cache L1/L2 da CPU durante multiplicações matriciais pesadas.
/// </summary>
type AssetName = string
type Weights = float array
type ExpectedReturns = float array
type CovarianceMatrix = float[,] // Matriz 2D nativa do .NET de alta performance

/// <summary>
/// Representa as características de risco e retorno de uma única carteira simulada.
/// Record estritamente imutável para garantir a pureza das funções de simulação.
/// </summary>
type PortfolioResult = {
    Weights: Weights
    ExpectedReturn: float
    Volatility: float
    SharpeRatio: float
}

/// <summary>
/// Representa um subconjunto específico de ativos (ex: 25 ativos escolhidos dentre os 30).
/// Carrega a matriz de covariância já pré-calculada para não repetir processamento.
/// </summary>
type AssetSubset = {
    Assets: AssetName array
    MeanReturns: ExpectedReturns
    Covariance: CovarianceMatrix
}

/// <summary>
/// Representa o resultado supremo da otimização após avaliar o espaço combinatório.
/// </summary>
type OptimizationResult = {
    SelectedAssets: AssetName array
    BestPortfolio: PortfolioResult
    TotalSimulations: int64
}