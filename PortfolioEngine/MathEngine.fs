module PortfolioEngine.MathEngine

open PortfolioEngine.Domain

// Funções puras (stubs iniciais para o compilador passar)
let calculateReturn (weights: Weights) (meanReturns: MeanReturns) : float =
    // TODO: Implementar w * r anualizado
    0.0 

let calculateVolatility (weights: Weights) (covMatrix: CovarianceMatrix) : float =
    // TODO: Implementar sqrt(wT * C * w) anualizado
    0.0 

let calculateSharpe (expectedReturn: float) (volatility: float) (riskFreeRate: float) : float =
    if volatility = 0.0 then 0.0 
    else (expectedReturn - riskFreeRate) / volatility