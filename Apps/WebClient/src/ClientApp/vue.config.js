const path = require("path");
const isDevBuild = true;
const cssLoader = "css-loader";
const styleLoader = "style-loader";
const bundleOutputDir = "/dist";
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
module.exports = {
    filenameHashing: false,
    css: {
        extract: false,
    },
    configureWebpack: {
        optimization: {
            splitChunks: false,
        },
        stats: { modules: false },
        context: __dirname,
        resolve: {
            extensions: [".js", ".ts", "vue", "json"],
            alias: {
                "@": path.resolve("src"),
                vue$: "vue/dist/vue.runtime.esm.js",
            },
        },
        entry: { main: "./src/main.ts" },
        module: {
            noParse: /^(vue|vue-router|vuex|vuex-router-sync)$/,
        },
    },
    chainWebpack: (config) => {
        config.plugins.delete("split-manifest").delete("inline-manifest");
    },
};
