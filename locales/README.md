# Traduções colaborativas

Os catálogos usam **chaves estáveis**. A chave nunca deve ser renomeada quando o texto for corrigido; somente o valor traduzido muda.

## Criar um idioma

1. Copie `en.json` e renomeie com o código do idioma, por exemplo `es.json`.
2. Altere `code`, `name` e `author`.
3. Traduza somente os valores de `translations`.
4. Preserve exatamente os placeholders, incluindo chaves e conteúdo, por exemplo `{count}`, `{filename}` e `{result.OutputDatPath}`.

As chaves ausentes usam português automaticamente. Uma tradução com placeholders incompatíveis também volta ao português e é registrada no diagnóstico, sem impedir a inicialização.

O sistema aplica o idioma à janela principal, janelas secundárias, menus, listas, mensagens dinâmicas, caixas de confirmação e seletores nativos de arquivos/pastas. A troca de idioma é imediata e reversível.

Reinicie o programa depois de adicionar um novo arquivo; o idioma aparecerá em **Configurações → Geral → Idioma da interface**.