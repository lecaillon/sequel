const MonacoEditorPlugin = require('monaco-editor-webpack-plugin')

module.exports = {
  transpileDependencies: ["vuetify"],
  configureWebpack: {
    plugins: [
      new MonacoEditorPlugin({
        languages: ['sql']
      })
    ]
  }
};
