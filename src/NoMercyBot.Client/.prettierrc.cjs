/** @type {import('@types/prettier').Config} */
module.exports = {
  semi: true,
  tabWidth: 4,
  useTabs: false,
  printWidth: 120,
  endOfLine: "auto",
  singleQuote: true,
  arrowParens: "avoid",
  bracketSpacing: true,
  trailingComma: "all",
  quoteProps: "as-needed",
  jsxBracketSameLine: false,
  overrides: [
    {
      files: ["*.ts", "*.vue", "*.js"],
      options: {
        parser: "typescript",
      },
    },
  ],
};
