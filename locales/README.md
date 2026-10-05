# Traduções colaborativas

Os catálogos usam **chaves estáveis**. A chave nunca deve ser renomeada quando o texto for corrigido; somente o valor traduzido muda.

## Padrão de chaves

Novas chaves devem descrever onde e como o texto é usado, sem copiar o texto exibido:

```text
modulo.[contexto.]componente.finalidade
```

- Use nomes em inglês, minúsculos e `snake_case` dentro de cada segmento.
- O contexto pode ser uma aba, janela ou seção e deve ser omitido quando não acrescentar informação.
- Use `common` somente para ações com o mesmo significado em toda a aplicação.
- Componentes recomendados: `btn`, `lbl`, `tab`, `menu`, `tooltip`, `placeholder`, `dialog`, `message`, `status`, `filter`, `option`, `group` e `column`.

Exemplos: `common.btn.cancel`, `characters.mesh.btn.rotate_x` e `visual_editor.collision.message.remove_confirm`.

No código, textos com chave semântica devem usar `LanguageService.T("chave")`. A tradução por correspondência do texto literal existe apenas para compatibilidade durante a migração.

## Criar um idioma

1. Copie `en.json` e renomeie com o código do idioma, por exemplo `es.json`.
2. Altere `code`, `name` e `author`.
3. Traduza somente os valores de `translations`.
4. Preserve exatamente os placeholders, incluindo chaves e conteúdo, por exemplo `{count}`, `{filename}` e `{result.OutputDatPath}`.

As chaves ausentes usam português automaticamente. Uma tradução com placeholders incompatíveis também volta ao português e é registrada no diagnóstico, sem impedir a inicialização.

O sistema aplica o idioma à janela principal, janelas secundárias, menus, listas, mensagens dinâmicas, caixas de confirmação e seletores nativos de arquivos/pastas. A troca de idioma é imediata e reversível.

Reinicie o programa depois de adicionar um novo arquivo; o idioma aparecerá em **Configurações → Geral → Idioma da interface**.
