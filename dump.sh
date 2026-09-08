#!/usr/bin/env bash

set -euo pipefail

stream="$1"

export EQUINOX_DYNAMO_SERVICE_URL="http://localhost:8000"
export EQUINOX_DYNAMO_ACCESS_KEY_ID="abc"
export EQUINOX_DYNAMO_SECRET_ACCESS_KEY="def"
export EQUINOX_DYNAMO_TABLE="eqx"

dotnet eqx dump "$stream" dynamo
