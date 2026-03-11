const webpackMerge = require("webpack-merge");
const path = require("path");
const fs = require("fs");
const spawn = require("child_process").spawn;
const TerserPlugin = require("terser-webpack-plugin");

const baseWebpackConfig = require("@kentico/xperience-webpack-config");

const { cert, key } = getDotnetCertPaths();

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      orgName: "baseline",
      projectName: "media-tools",
      webpackConfigEnv,
      argv,
    });
  };

  return new Promise((resolve) => {
    if (
      argv.mode === "production" ||
      (fs.existsSync(cert) && fs.existsSync(key))
    ) {
      console.info("Skipping dev certification creation");
      resolve(buildConfig(baseConfig, opts, argv));
      return;
    }

    console.info("Creating aspnet core dev-certs");
    spawn(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        cert,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" },
    ).on("exit", (code) => {
      resolve(buildConfig(baseConfig, opts, argv));
      if (code) {
        process.exit(code);
      }
    });
  });
};

function buildConfig(baseConfig, opts, argv) {
  const projectConfig = {
    devtool: "inline-source-map",
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: "babel-loader",
        },
        {
          test: /\.css$/i,
          use: ["style-loader", "css-loader"],
        },
      ],
    },
    output: {
      clean: true,
      chunkFilename: "chunk.kxh.[contenthash].js",
    },
    optimization: {
      splitChunks: false,
      runtimeChunk: false,
      minimizer: [
        new TerserPlugin({
          extractComments: false,
        }),
      ],
    },
    devServer: {
      port: 3021,
      server: {
        type: "http",
      },
    },
    resolve: {
      modules: [path.resolve(__dirname, "node_modules"), "node_modules"],
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
}

function getDotnetCertPaths() {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ""
      ? `${process.env.APPDATA}/ASP.NET/https`
      : `${process.env.HOME}/.aspnet/https`;

  const certificateName = process.env.npm_package_name;

  const cert = path.join(baseFolder, `${certificateName}.pem`);
  const key = path.join(baseFolder, `${certificateName}.key`);

  return { cert, key };
}
