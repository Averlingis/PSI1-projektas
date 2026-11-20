#!/usr/bin/env bash
# Runs the tests with coverage and creates an HTML report in coveragereport/.
# Run from the repository root: ./scripts/coverage.sh
set -e

rm -rf coverage coveragereport

dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/*.cs,**/obj/**"

reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html

start coveragereport/index.html