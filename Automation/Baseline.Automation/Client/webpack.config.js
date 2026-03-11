const webpackMerge = require("webpack-merge");
const baseWebpackConfig = require("@kentico/xperience-webpack-config");
const webpack = require("webpack");

module.exports = (opts, argv) => {
  const isProduction = argv.mode === "production";
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      orgName: "baseline",
      projectName: "automation",
      webpackConfigEnv: webpackConfigEnv,
      argv: argv,
    });
  };

  const projectConfig = {
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: "babel-loader",
        },
        {
          test: /\.module\.css$/,
          use: [
            "style-loader",
            {
              loader: "css-loader",
              options: {
                modules: {
                  localIdentName: "[local]___[hash:base64:5]",
                  namedExport: false,
                },
              },
            },
          ],
        },
        {
          test: /\.css$/,
          exclude: /\.module\.css$/,
          use: ["style-loader", "css-loader"],
        },
      ],
    },
    plugins: [
      new webpack.DefinePlugin({
        "process.env.NODE_ENV": JSON.stringify(
          isProduction ? "production" : "development",
        ),
      }),
    ],
    output: {
      clean: true,
    },
    devServer: {
      port: 3022,
    },
  };

  return webpackMerge.merge(baseConfig(opts, argv), projectConfig);
};
