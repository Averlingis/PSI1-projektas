#!/usr/bin/env bash
# Runs the tests with coverage and creates an HTML report in coveragereport/ and opens it.
# Run from the repository root: ./scripts/coverage.sh
set -e

rm -rf coverage coveragereport

dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/*.cs,**/obj/**"

reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html

case "$(uname -s)" in
  Linux*)               xdg-open coveragereport/index.html ;;
  Darwin*)              open coveragereport/index.html ;;
  MINGW*|MSYS*|CYGWIN*) start "" coveragereport/index.html ;;
esac
