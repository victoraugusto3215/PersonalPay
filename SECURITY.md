# Política de Segurança

## Escopo

O PersonalPay integra um gateway de pagamento real (Mercado Pago) e a
WhatsApp Business API, e processa webhooks de notificação de pagamento.
Falhas que envolvam validação de assinatura de webhook, idempotência de
notificação, exposição de credenciais ou acesso indevido a cobranças de
outro personal trainer são tratadas como críticas.

## Versões suportadas

Projeto com um único ambiente de produção. Apenas o código na branch `main`
recebe correções de segurança.

| Versão            | Suportada          |
| ------------------ | ------------------ |
| `main` (mais recente) | :white_check_mark: |
| Commits anteriores | :x:                 |

## Reportando uma vulnerabilidade

**Não abra uma issue pública para vulnerabilidades de segurança.**

Reporte diretamente para **victoraugusto3215@gmail.com** com:

- Descrição do problema e impacto potencial (ex.: bypass da validação de
  `x-signature` do webhook, replay de notificação, exposição de token do
  Mercado Pago/WhatsApp, acesso a dados de outro aluno/cliente).
- Passos para reproduzir, se possível.
- Versão/commit afetado.

Você deve receber uma resposta inicial em até 72 horas. O objetivo é
publicar uma correção antes de qualquer divulgação pública, com crédito ao
reportante (a menos que prefira anonimato).

## Fora de escopo

- Este projeto não armazena dados de cartão nem processa pagamentos
  diretamente — toda a movimentação financeira acontece na API do Mercado
  Pago; o PersonalPay só consulta status e webhooks de notificação.
- Disponibilidade/uptime da infraestrutura de terceiros (Mercado Pago, Meta
  WhatsApp Cloud API) está fora do escopo deste repositório.
