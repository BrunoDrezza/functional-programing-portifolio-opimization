import pandas as pd
import numpy as np
import matplotlib
matplotlib.use('Agg') # Trava para o WSL
import matplotlib.pyplot as plt
import scipy.optimize as sco
import os
import re
from pathlib import Path

# Define o caminho para a raiz do projeto (um nível acima da pasta scripts)
BASE_DIR = Path(__file__).resolve().parent.parent

print("1. Carregando dados e convertendo preços em retornos...")
# Em vez de '../data/...'
mc_df = pd.read_csv(BASE_DIR / 'data' / 'efficient_frontier.csv')

prices_df = pd.read_csv(BASE_DIR / 'data' / 'data_raw' / 'all_returns.csv', index_col='Date')
returns_df = prices_df.pct_change().dropna()
mean_returns = returns_df.mean(numeric_only=True).to_numpy(dtype=float) * 252
cov_matrix = returns_df.cov(numeric_only=True).to_numpy(dtype=float) * 252
assets = returns_df.columns
n_assets = len(assets)
risk_free_rate = 0.0

print("2. Calculando as métricas Analíticas (SciPy com limite de 20%)...")
bounds = tuple((0.0, 0.20) for _ in range(n_assets))

# A) Mínima Variância Analítica (Ponta Esquerda da Linha)
res_min_vol = sco.minimize(
    lambda w: np.sqrt(w.T @ cov_matrix @ w), 
    np.ones(n_assets)/n_assets, method='SLSQP', bounds=bounds, 
    constraints={'type': 'eq', 'fun': lambda w: np.sum(w) - 1}
)
opt_min_vol = res_min_vol.fun
opt_min_ret = res_min_vol.x.T @ mean_returns
opt_min_sharpe = (opt_min_ret - risk_free_rate) / opt_min_vol

# B) Máximo Retorno Analítico (Ponta Direita da Linha)
res_max_ret = sco.minimize(
    lambda w: -(w.T @ mean_returns), 
    np.ones(n_assets)/n_assets, method='SLSQP', bounds=bounds, 
    constraints={'type': 'eq', 'fun': lambda w: np.sum(w) - 1}
)
max_ret = -(res_max_ret.fun)

# C) Máximo Sharpe Analítico (Tangência)
def negative_sharpe(w):
    ret = w.T @ mean_returns
    vol = np.sqrt(w.T @ cov_matrix @ w)
    return -(ret - risk_free_rate) / vol

res_max_sharpe = sco.minimize(
    negative_sharpe, 
    np.ones(n_assets)/n_assets, method='SLSQP', bounds=bounds, 
    constraints={'type': 'eq', 'fun': lambda w: np.sum(w) - 1}
)
opt_max_ret = res_max_sharpe.x.T @ mean_returns
opt_max_vol = np.sqrt(res_max_sharpe.x.T @ cov_matrix @ res_max_sharpe.x)
opt_max_sharpe = (opt_max_ret - risk_free_rate) / opt_max_vol

print("3. Traçando a Linha da Fronteira Eficiente...")
targets = np.linspace(opt_min_ret, max_ret, 50)
frontier_vols = []

init_guess = res_min_vol.x
for tr in targets:
    res = sco.minimize(
        lambda w: np.sqrt(w.T @ cov_matrix @ w), 
        init_guess, method='SLSQP', bounds=bounds, 
        constraints=(
            {'type': 'eq', 'fun': lambda w: np.sum(w) - 1}, 
            {'type': 'eq', 'fun': lambda w: w.T @ mean_returns - tr}
        )
    )
    frontier_vols.append(res.fun)
    init_guess = res.x

print("4. Extraindo Carteiras Extremas do Monte Carlo...")
# Puxando o Máximo Sharpe do F# direto do CSV gerado!
max_sharpe_idx = mc_df['Sharpe'].idxmax()
campea_mc = mc_df.loc[max_sharpe_idx]

min_vol_idx = mc_df['Volatility'].idxmin()
min_vol_mc = mc_df.loc[min_vol_idx]

print("\n================ COMPARAÇÃO DOS MODELOS ================")
print("--- CARTEIRA DE MÁXIMO SHARPE (Tangência) ---")
print(f"F# Monte Carlo -> Sharpe: {campea_mc['Sharpe']:.4f} | Retorno: {campea_mc['Return']*100:.2f}% | Risco: {campea_mc['Volatility']*100:.2f}%")
print(f"Python SciPy   -> Sharpe: {opt_max_sharpe:.4f} | Retorno: {opt_max_ret*100:.2f}% | Risco: {opt_max_vol*100:.2f}%")

print("\n5. Desenhando o Gráfico Comparativo...")
plt.figure(figsize=(12, 8))

plt.scatter(mc_df['Volatility'], mc_df['Return'], c=mc_df['Sharpe'], 
            cmap='viridis', marker='o', s=10, alpha=0.3, label='Simulações Monte Carlo (F#)')
plt.colorbar(label='Sharpe Ratio')

plt.plot(frontier_vols, targets, color='#39ff14', linewidth=3, 
         linestyle='-', label='Fronteira Eficiente Analítica (Restrição 20%)')

plt.scatter(campea_mc['Volatility'], campea_mc['Return'], color='red', 
            marker='*', s=350, edgecolor='black', zorder=5,
            label=f"Max Sharpe F# ({campea_mc['Sharpe']:.2f})")

plt.scatter(opt_max_vol, opt_max_ret, color='cyan', 
            marker='*', s=350, edgecolor='black', zorder=6,
            label=f"Max Sharpe SciPy ({opt_max_sharpe:.2f})")

plt.scatter(min_vol_mc['Volatility'], min_vol_mc['Return'], color='orange', 
            marker='D', s=150, edgecolor='black', zorder=5,
            label=f"Min Vol F# ({min_vol_mc['Volatility']*100:.2f}%)")

plt.scatter(opt_min_vol, opt_min_ret, color='magenta', 
            marker='D', s=150, edgecolor='black', zorder=6,
            label=f"Min Vol SciPy ({opt_min_vol*100:.2f}%)")

plt.title('Otimização Dow Jones: Monte Carlo (F#) vs Analítico (SciPy)', fontsize=16, fontweight='bold')
plt.xlabel(r'Volatilidade / Risco Anualizado ($\sigma$)', fontsize=12)
plt.ylabel(r'Retorno Esperado Anualizado ($E[R]$)', fontsize=12)
plt.grid(True, linestyle='--', alpha=0.3)
plt.legend(loc='lower right', frameon=True, facecolor='black', edgecolor='white', labelcolor='white')

plt.gca().set_facecolor('#1e1e1e')
plt.gcf().patch.set_facecolor('#1e1e1e')
plt.gca().xaxis.label.set_color('white')
plt.gca().yaxis.label.set_color('white')
plt.gca().title.set_color('white')
plt.tick_params(colors='white')

output_path = BASE_DIR / 'data' / 'efficient_frontier_dual.png'
plt.savefig(output_path, dpi=300, bbox_inches='tight', facecolor='#1e1e1e')

print("\n6. Automatizando a Escrita do README.md...")
readme_path = BASE_DIR / 'README.md'

if os.path.exists(readme_path):
    with open(readme_path, 'r', encoding='utf-8') as f:
        readme_content = f.read()
        
    # Magia Regex: Localiza a seção de métricas e injeta os números exatos do Python e do F#
    pattern = r"\*\*A\. Comparativo de Máximo Sharpe \(Tangência\):\*\*.*?\*\*B\. Conclusão sobre a Divergência:\*\*"
    
    replacement = f"""**A. Comparativo de Máximo Sharpe (Tangência):**
* **Método Analítico (Otimização Matemática):**
  * Sharpe Ratio: **{opt_max_sharpe:.4f}**
  * Retorno Esperado: **{opt_max_ret*100:.2f}**%
  * Risco (Volatilidade): **{opt_max_vol*100:.2f}**%
* **Método Heurístico (Monte Carlo - F#):**
  * Sharpe Ratio: **{campea_mc['Sharpe']:.4f}**
  * Retorno Esperado: **{campea_mc['Return']*100:.2f}**%
  * Risco (Volatilidade): **{campea_mc['Volatility']*100:.2f}**%

**B. Conclusão sobre a Divergência:**"""

    new_readme = re.sub(pattern, replacement, readme_content, flags=re.DOTALL)
    
    with open(readme_path, 'w', encoding='utf-8') as f:
        f.write(new_readme)
    print("✅ README.md atualizado e preenchido automaticamente!")
else:
    print("⚠️ Arquivo README.md não encontrado. Crie o arquivo na pasta principal para auto-preencher.")

print(f"✅ Sucesso Absoluto! Gráfico comparativo salvo em: {output_path}")