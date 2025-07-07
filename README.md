# .NET Event 2025/06/30 - AI Sample Collection

This repository contains a collection of AI-powered applications and samples demonstrating various Microsoft AI technologies with .NET 9.0, including Microsoft Extensions AI, Azure OpenAI, Semantic Kernel, and vector databases.

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for authentication)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for VectorDataApp)

## 🚀 Projects Overview

### 1. ChatClientHelloWorld
A simple console application demonstrating how to use Microsoft Extensions AI with Azure OpenAI for chat completion and function calling.

**Features:**
- Azure OpenAI chat completion
- Azure AI Inference integration
- Function calling with tools (current date/time)
- Logging and telemetry

**Key Technologies:**
- Microsoft.Extensions.AI
- Azure.AI.OpenAI
- Azure.AI.Inference

### 2. EmbeddingHelloWorld
A console application showcasing text embedding generation and similarity calculations using Azure OpenAI.

**Features:**
- Text embedding generation
- Cosine similarity calculation
- Multi-language text comparison (Japanese/English)

**Key Technologies:**
- Microsoft.Extensions.AI
- Azure.AI.OpenAI
- Text embedding models

### 3. EvaluationTest
A comprehensive test suite for evaluating AI model performance using Microsoft Extensions AI Evaluation framework.

**Features:**
- AI model evaluation and comparison
- Quality metrics assessment
- Reporting and analytics
- Multiple model testing (GPT-4o-mini, GPT-4.1, DeepSeek, O3)

**Key Technologies:**
- Microsoft.Extensions.AI.Evaluation
- MSTest framework
- Azure OpenAI models

### 4. SKAgent
A Semantic Kernel-based agent system for content creation and research.

**Features:**
- Multiple specialized agents (Researcher, Planner, Writer, Finalizer)
- Azure AI Foundry integration
- Multi-step content creation workflow
- Agent orchestration

**Key Technologies:**
- Microsoft.SemanticKernel
- Azure.AI.Projects
- Agent framework

### 5. SKMultiAgent (SKProcess)
An advanced multi-agent system using Semantic Kernel with process orchestration.

**Features:**
- Process-based agent coordination
- Dependency injection integration
- Advanced workflow management
- Multi-step AI processes

**Key Technologies:**
- Microsoft.SemanticKernel
- Dependency injection
- Process orchestration

### 6. VectorDataApp
A comprehensive AI chat application with vector database integration for chatting with custom documents.

**Features:**
- Vector database integration (Qdrant)
- PDF document ingestion
- Semantic search capabilities
- Real-time chat interface
- .NET Aspire integration

**Key Technologies:**
- .NET Aspire
- Qdrant vector database
- Blazor Server
- Microsoft.Extensions.AI

> **Note:** VectorDataApp has its own detailed [README.md](./VectorDataApp/README.md) with specific setup instructions.

## 🔧 Configuration

### Azure OpenAI Setup
All projects require Azure OpenAI configuration. You'll need:

1. **Azure OpenAI Service resource**
2. **Deployed models:**
   - `gpt-4o-mini`
   - `gpt-4.1` 
   - `o3` (if available)
   - `text-embedding-3-large` or `text-embedding-3-small`

### User Secrets Configuration
Each project uses .NET User Secrets for secure configuration. Set up the following secrets:

```bash
# For most projects
dotnet user-secrets set "AzureAIFoundry:Endpoint" "https://your-resource.openai.azure.com/"

# For projects using Azure AI Foundry Agents
dotnet user-secrets set "AzureAIFoundry:AgentEndpoint" "https://your-agent-endpoint"
dotnet user-secrets set "AzureAIFoundry:AgentId" "your-agent-id"

# For DeepSeek integration
dotnet user-secrets set "AzureAIFoundry:DeepSeekEndpoint" "https://your-deepseek-endpoint"
```

## 🏃 Running the Projects

### Console Applications (ChatClientHelloWorld, EmbeddingHelloWorld, SKAgent, SKMultiAgent)
```bash
cd [ProjectName]
dotnet run
```

### Test Project (EvaluationTest)
```bash
cd EvaluationTest
dotnet test
```

### VectorDataApp
```bash
cd VectorDataApp
dotnet run --project VectorDataApp.AppHost
```

## 📚 Learn More

- [Microsoft Extensions AI Documentation](https://learn.microsoft.com/dotnet/ai/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)

## 🤝 Contributing

This repository serves as a collection of samples and demos. Feel free to explore, modify, and adapt the code for your own AI projects.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.