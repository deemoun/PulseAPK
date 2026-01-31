# PulseAPK

**PulseAPK** é uma GUI de nível profissional para engenharia reversa e análise de segurança no Android, construída com WPF e .NET 8. Ela combina o poder bruto do `apktool` com recursos avançados de análise estática, envoltos em uma interface de alto desempenho com inspiração cyberpunk. O PulseAPK simplifica todo o fluxo de trabalho, da decompilação à análise, reconstrução e assinatura.

[Assista à demo no YouTube](https://youtu.be/Mkdt0c-7Wwg)

![PulseAPK UI](images/apktool_decompile.png)

Você também pode realizar a análise de código Smali. Basta arrastar e soltar a pasta Smali na aba Analysis.

![PulseAPK Smali Analysis](images/apktool_analysis.png)

Se você quiser compilar (e assinar, se necessário) a pasta Smali, use a seção "Build APK".

![PulseAPK Build APK](images/apktool_build_apk.png)

## Principais recursos

- **🛡️ Análise de segurança estática**: Varre automaticamente o código Smali em busca de vulnerabilidades, incluindo detecção de root, verificações de emulador, credenciais codificadas e uso inseguro de SQL/HTTP.
- **⚙️ Motor de regras dinâmico**: Regras de análise totalmente personalizáveis via `smali_analysis_rules.json`. Modifique padrões de detecção em tempo real sem reiniciar o aplicativo. Usa cache para desempenho ideal.
- **🚀 UI/UX moderno**: Interface escura e responsiva projetada para eficiência, com suporte a arrastar e soltar e feedback de console em tempo real.
- **📦 Fluxo de trabalho completo**: Descompilar, analisar, editar, recompilar e assinar APKs em um ambiente unificado.
- **⚡ Seguro e robusto**: Inclui validação inteligente e prevenção de falhas para proteger seu workspace e dados.
- **🔧 Totalmente configurável**: Gerencie caminhos de ferramentas (Java, Apktool), configurações do workspace e parâmetros de análise com facilidade.

## Capacidades avançadas

### Análise de segurança
O PulseAPK inclui um analisador estático embutido que varre o código decompilado em busca de indicadores de segurança comuns:
- **Detecção de root**: Identifica verificações para Magisk, SuperSU e binários de root comuns.
- **Detecção de emulador**: Encontra verificações para QEMU, Genymotion e propriedades específicas do sistema.
- **Dados sensíveis**: Varre chaves de API, tokens e cabeçalhos basic auth codificados.
- **Rede insegura**: Sinaliza o uso de HTTP e possíveis pontos de vazamento de dados.

*As regras são definidas em `smali_analysis_rules.json` e podem ser personalizadas conforme suas necessidades.*

### Gerenciamento de APK
- **Descompilação**: Decodifique recursos e fontes com opções configuráveis.
- **Recompilação**: Reconstrua seus projetos modificados em APKs válidos.
- **Assinatura**: Gerenciamento integrado de keystore para assinar APKs recompilados, garantindo que estejam prontos para instalação no dispositivo.

## Pré-requisitos

1.  **Java Runtime Environment (JRE)**: Necessário para `apktool`. Garanta que `java` esteja no seu `PATH`.
2.  **Apktool**: Baixe `apktool.jar` em [ibotpeaches.github.io](https://ibotpeaches.github.io/Apktool/).
3.  **Ubersign (Uber APK Signer)**: Necessário para assinar APKs recompilados. Baixe a versão mais recente de `uber-apk-signer.jar` nos [releases do GitHub](https://github.com/patrickfav/uber-apk-signer/releases).
4.  **.NET 8.0 Runtime**: Necessário para executar o PulseAPK no Windows.

## Guia de início rápido

1.  **Baixar e compilar**
    ```powershell
    dotnet build
    dotnet run
    ```

2.  **Configuração**
    - Abra **Settings**.
    - Informe o caminho para `apktool.jar`.
    - O PulseAPK detectará automaticamente sua instalação do Java com base nas variáveis de ambiente.

3.  **Analisar um APK**
    - **Descompile** seu APK alvo na aba Decompile.
    - Vá para a aba **Analysis**.
    - Selecione a pasta do projeto decompilado.
    - Clique em **Analyze Smali** para gerar um relatório de segurança.

4.  **Modificar e recompilar**
    - Edite arquivos na pasta do projeto.
    - Use a aba **Build** para recompilar em um novo APK.
    - Use a aba **Sign** para assinar o APK de saída.

## Arquitetura técnica

O PulseAPK utiliza uma arquitetura MVVM (Model-View-ViewModel) limpa:

- **Core**: .NET 8.0, WPF.
- **Analysis**: Motor de análise estática personalizado baseado em regex com regras de recarga a quente.
- **Services**: serviços dedicados para interação com Apktool, monitoramento do sistema de arquivos e gerenciamento de configurações.

## Licença

Este projeto é open source e está disponível sob a [Apache License 2.0](LICENSE.md).

### ❤️ Apoie o projeto

Se o PulseAPK for útil para você, pode apoiar seu desenvolvimento clicando no botão "Support" no topo.

Dar uma estrela ao repositório também ajuda bastante.

### Contribuições

Aceitamos contribuições! Observe que todos os colaboradores devem assinar nosso [Contributor License Agreement (CLA)](CLA.md) para que seu trabalho possa ser distribuído legalmente.
Ao enviar um pull request, você concorda com os termos do CLA.
