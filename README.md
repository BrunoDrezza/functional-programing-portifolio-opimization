# Otimizador de Portfólio Dow Jones (Monte Carlo)

Este projeto consiste em um motor de otimização de carteiras de alto desempenho, desenvolvido em **F#**, para identificar a alocação de ativos que maximiza o **Sharpe Ratio**. O sistema utiliza simulações de Monte Carlo massivamente paralelas para explorar o espaço combinatório dos ativos do índice Dow Jones (DJIA).

O projeto foi desenvolvido como requisito final para a disciplina de **Programação Funcional** no **Insper (2026-1)**.

## 1. Contexto e Objetivos

O desafio proposto por um gestor de portfólio consiste em selecionar a melhor combinação de **25 ou mais ativos** entre os 30 disponíveis no Dow Jones. Para cada combinação, o sistema deve realizar **1.000.000 de simulações** de pesos aleatórios para encontrar a Fronteira Eficiente.

### Regras e Restrições:

* **Universo**: 30 ações do Dow Jones Industrial Average.
* **Janela de Dados**: Segundo semestre de 2025 (01/07/2025 a 31/12/2025).
* **Estratégia**: *Long-only* ($w_i \ge 0$) com soma dos pesos igual a 1.
* **Concentração**: Máximo de 20% por ativo ($w_i \le 0.2$).
* **Métrica Alvo**: Sharpe Ratio (anualizado).

## 2. Arquitetura do Projeto

Baseado em princípios de **Clean Architecture** e **Programação Funcional**, o projeto é dividido em uma solução .NET com três componentes principais, isolando a lógica pura dos efeitos colaterais.

### Estrutura da Solution:

* **`PortfolioEngine` (Library)**: Contém o "núcleo duro" matemático. É uma biblioteca puramente funcional, sem I/O ou estado compartilhado.
* `Domain.fs`: Definições de tipos imutáveis e modelos de dados.
* `MathEngine.fs`: Funções puras para cálculo de retorno, matriz de covariância e volatilidade.
* `Simulator.fs`: Motor de simulação de Monte Carlo e lógica de geração de pesos.


* **`Main` (Console App)**: O orquestrador do sistema. Lida com funções impuras.
* `DataLoader.fs`: Módulo responsável pelo consumo da API (Yahoo Finance) e parsing de CSVs.
* `Program.fs`: Ponto de entrada, gerenciamento de paralelismo e exibição de resultados.


* **`PortfolioEngine.Tests` (xUnit)**: Suite de testes unitários para validar a precisão dos cálculos matemáticos.

## 3. Fundamentação Matemática

A otimização busca maximizar a função objetivo:


$$SR = \frac{\mu - r_{free}}{\sigma}$$

Onde:

* **Retorno ($\mu$)**: Multiplicação matricial dos pesos pelos retornos médios históricos, anualizada por 252 dias.
* **Volatilidade ($\sigma$)**: Cálculo via forma quadrática $\sigma_p = \sqrt{w^T \cdot C \cdot w}$, onde $C$ é a matriz de covariância, anualizada por $\sqrt{252}$.

## 4. Tecnologias Utilizadas

* **Linguagem**: F# (Paradigma Funcional).
* **Runtime**: .NET 8 / .NET 9.
* **Ambiente**: Desenvolvido e otimizado para **Linux (WSL2)**.
* **Paralelismo**: Uso de `Array.Parallel` e workflows assíncronos para distribuição de carga em múltiplos núcleos de CPU.
* **Bibliotecas**: `FSharp.Data` (para manipulação de JSON/CSV) e `MathNet.Numerics` (álgebra linear otimizada).

## 5. Como Instalar e Rodar

### Pré-requisitos:

* [.NET SDK](https://dotnet.microsoft.com/download) instalado.
* Terminal Bash (Linux ou WSL).

### Instalação:

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/portfolio-optimizer-fsharp.git
cd portfolio-optimizer-fsharp

# Restaure as dependências
dotnet restore

```

### Execução:

Para rodar o otimizador principal:

```bash
dotnet run --project Main

```

Para rodar os benchmarks de performance (paralelo vs serial):

```bash
./scripts/run_benchmark.sh

```

## 6. Testes Unitários

Para garantir a integridade dos cálculos de risco e retorno, execute a suite de testes:

```bash
dotnet test

```

## 7. Desenvolvimento e Autoria

Este projeto foi desenvolvido de forma estritamente individual. O histórico de commits reflete o processo iterativo de construção da arquitetura funcional, migração de dados e otimização de performance.