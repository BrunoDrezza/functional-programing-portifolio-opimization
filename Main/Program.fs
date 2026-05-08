open System
open PortfolioEngine.Domain
open PortfolioEngine.Simulator

[<EntryPoint>]
let main argv =
    Console.WriteLine("Motor de Otimização Dow Jones iniciado com sucesso!")
    
    // Teste simples puxando uma função da biblioteca
    let weights = generateValidWeights 25
    printfn "Pesos iniciais gerados: %A" weights
    
    0 // Código de saída padrão