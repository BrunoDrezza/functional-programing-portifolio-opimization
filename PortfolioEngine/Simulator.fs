module PortfolioEngine.Simulator

open PortfolioEngine.Domain

// Onde a lógica híbrida de Rejection Sampling + Iterative Redistribution vai morar
let generateValidWeights (numAssets: int) : Weights =
    // TODO: Lógica de sorteio garantindo w_i <= 0.2 e soma = 1
    Array.create numAssets (1.0 / float numAssets)