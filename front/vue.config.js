const MonacoEditorPlugin = require('monaco-editor-webpack-plugin')

module.exports = {
  transpileDependencies: ["vuetify"],
  configureWebpack: {
    plugins: [
      new MonacoEditorPlugin({
        languages: ['sql'],
        features: [
          '!accessibilityHelp',
          '!bracketMatching',
          'caretOperations',
          'clipboard',
          '!codeAction',
          '!codelens',
          '!colorDetector',
          'comment',
          'contextmenu',
          'coreCommands',
          'cursorUndo',
          '!dnd',
          'find',
          '!folding',
          'fontZoom',
          '!format',
          '!gotoError',
          'gotoLine',
          '!gotoSymbol',
          '!hover',
          '!inspectTokens',
          'linesOperations',
          '!links',
          'multicursor',
          '!parameterHints',
          '!quickCommand',
          '!quickOutline',
          '!referenceSearch',
          '!rename',
          '!smartSelect',
          'snippets',
          'suggest',
          '!toggleHighContrast',
          '!toggleTabFocusMode',
          '!transpose',
          'wordHighlighter',
          'wordOperations',
          '!wordPartOperations']
      })
    ]
  }
};