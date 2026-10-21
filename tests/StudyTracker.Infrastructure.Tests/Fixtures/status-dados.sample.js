// Cabeçalho de teste.
// Segunda linha.
window.DADOS = {
  "atualizadoEm": "2026-03-02",
  "historico": [
    { "data": "2026-03-02", "texto": "XY1 TP2: Falta contexto para Pronto p/ começar." },
    { "data": "2026-03-01", "texto": "Texto longo o bastante para não caber numa linha só de cento e dez caracteres, com acentuação: ção, ã, é." },
    { "data": "2026-02-28", "texto": "Aspas \"duplas\", barra \\ e quebra\nde linha, tab\taqui e controle \u0001." }
  ],
  "blocos": [
    {
      "nome": "Bloco Exemplo",
      "codigo": "26E1-26E2",
      "pasta": "Bloco Exemplo [26E1-26E2]",
      "atual": true,
      "grupos": [
        {
          "titulo": "26E2",
          "detalhe": "trimestre atual",
          "disciplinas": [
            {
              "sigla": "XY1",
              "nome": "Disciplina Exemplo",
              "pasta": "XY1 - Disciplina Exemplo [26E2]",
              "professor": "Professor Exemplo",
              "trabalhos": [
                {
                  "id": "TP1",
                  "status": "para-entregar",
                  "prazo": "2026-03-10",
                  "resumo": "Resumo do trabalho.",
                  "etapas": { "enunciado": true, "rubricas": true, "definicao": true, "guia": true, "artefatos": true },
                  "proximo": "Postar no Moodle.",
                  "guia": "Guia_TP1.md"
                },
                {
                  "id": "TP2",
                  "status": "entregue",
                  "prazo": "2026-03-20",
                  "entrega": "2026-03-18",
                  "resumo": "Ordem de campos diferente.",
                  "etapas": { "enunciado": true, "rubricas": true, "definicao": true, "guia": true, "artefatos": true },
                  "correcao": "Ainda não recebida.",
                  "guia": "Guia_TP2.md"
                },
                {
                  "id": "TP3",
                  "status": "sem-contexto",
                  "semPasta": true,
                  "proximo": "Mandar enunciado e rubricas do TP3.",
                  "obs": "Campo semPasta antes de proximo."
                },
                {
                  "id": "AT",
                  "status": "sem-contexto",
                  "proximo": "Mandar enunciado e rubricas da AT.",
                  "campoNovo": {
                    "qualquer": 1,
                    "lista": [1, 2]
                  }
                }
              ]
            }
          ]
        },
        {
          "titulo": "Projeto de Bloco",
          "detalhe": "bloco inteiro",
          "disciplinas": [
            {
              "sigla": "PB",
              "nome": "Projeto de Bloco",
              "pasta": "PB - Projeto de Bloco",
              "trabalhos": [
                { "id": "TP1", "status": "entregue" }
              ]
            }
          ]
        }
      ]
    },
    {
      "nome": "Bloco Antigo",
      "codigo": "25E3-25E4",
      "pasta": "Bloco Antigo [25E3-25E4]",
      "concluido": [
        {
          "sigla": "AB1",
          "nome": "Disciplina Antiga",
          "pasta": "AB1 - Disciplina Antiga [25E3]",
          "trabalhos": ["TP1", "TP2", "TP3", "AT"]
        }
      ]
    }
  ]
};
