🎬 Video Converter

Plataforma de conversão de vídeos MP4 para MP3, baseada em arquitetura de microserviços, processamento assíncrono e mensageria, projetada para ser escalável, desacoplada e resiliente.

📌 Visão Geral

Este projeto implementa um fluxo distribuído onde o usuário faz upload de um vídeo, o sistema processa a conversão de forma assíncrona e, ao final, notifica o usuário quando o arquivo MP3 estiver pronto.

A aplicação foi construída com foco em:

Separação clara de responsabilidades

Escalabilidade horizontal

Uso correto de bancos para cada tipo de dado

Infraestrutura versionada

🧱 Arquitetura
🔹 Componentes

GatewayService (API pública)

AuthService (autenticação e JWT)

ConverterService (worker de conversão)

NotificationService (worker de notificação)

RabbitMQ (mensageria)

PostgreSQL (dados relacionais)

MongoDB (armazenamento de arquivos via GridFS)

🔹 Estilo Arquitetural

Event-driven

Microserviços

Workers assíncronos

Comunicação via HTTP + mensageria

🔄 Fluxo da Aplicação

O cliente envia um vídeo MP4 para o GatewayService

O Gateway:

Valida o JWT chamando o AuthService

Armazena o vídeo no MongoDB (GridFS)

Publica uma mensagem na fila video_queue (RabbitMQ)

O ConverterService:

Consome a fila video_queue

Baixa o vídeo do MongoDB

Executa o FFmpeg para converter MP4 → MP3

Salva o MP3 no MongoDB

Publica evento de conclusão

O NotificationService:

Consome a fila mp3_queue

Envia um e-mail ao usuário informando que o MP3 está pronto

🛠️ Tecnologias Utilizadas
Backend

.NET (ASP.NET Core)

Entity Framework Core

Workers com BackgroundService

Infraestrutura

Docker & Docker Compose

RabbitMQ

PostgreSQL

MongoDB (GridFS)

Outros

JWT (Autenticação)

FFmpeg

MailKit (SMTP)

📂 Estrutura do Projeto
Infrastructure/
 └── docker-compose.yml

src/
 ├── AuthService
 │   ├── Models
 │   ├── Data
 │   ├── DTOs
 │   └── Controllers
 │
 ├── GatewayService
 │   ├── Controllers
 │   └── Services
 │
 ├── ConverterService
 │   ├── Services
 │   └── Worker.cs
 │
 └── NotificationService
     ├── Services
     └── Worker.cs

VideoConverter.sln

⚙️ Configuração do Ambiente
Pré-requisitos

Docker

Docker Compose

.NET SDK

FFmpeg instalado no sistema

Subindo a Infraestrutura
docker-compose up -d


Serviços disponíveis:

RabbitMQ: http://localhost:15672

PostgreSQL: localhost:5432

MongoDB: localhost:27017

🔐 Autenticação

Autenticação baseada em JWT

O AuthService atua como Identity Provider interno

Outros serviços validam tokens chamando o endpoint /validate

📬 Mensageria

video_queue
Responsável por disparar o processamento de conversão

mp3_queue
Responsável por disparar notificações ao usuário

RabbitMQ é usado para:

Desacoplamento

Tolerância a falhas

Processamento assíncrono

📦 Armazenamento

PostgreSQL

Usuários

Credenciais

Roles

MongoDB (GridFS)

Vídeos MP4

Áudios MP3

Cada banco é usado de forma intencional, respeitando seu propósito.

📈 Escalabilidade

Este projeto foi pensado para escalar:

Workers podem ser replicados

Serviços são stateless

Mensageria permite backpressure

Infra pronta para Kubernetes no futuro

🚀 Possíveis Evoluções

Deploy em Kubernetes

Observabilidade (Prometheus + Grafana)

Retry e DLQ no RabbitMQ

Autorização baseada em roles

Download autenticado do MP3

API Gateway dedicado

🧠 Considerações Finais

Este não é um CRUD simples.
É um projeto focado em arquitetura real de backend, usando padrões encontrados em sistemas de produção.

O objetivo é demonstrar:

Capacidade de design arquitetural

Domínio de microserviços

Uso correto de mensageria

Integração entre serviços

Infra como código
