STACK_NAME=sam-app-otel-rds-mssql

.PHONY: build

build:
	sam build

deploy: build
	$(info Executing sam deploy...no echo")
	@sam deploy \
		--capabilities CAPABILITY_NAMED_IAM \
		--parameter-overrides "newRelicLicenseKey=${NEW_RELIC_LICENSE_KEY}" "newRelicEndpoint=otlp.nr-data.net:4317" "mssqlDbEndpoint=${MSSQL_DB_ENDPOINT}" "mssqlDatabase=${MSSQL_DATABASE}" "mssqlUsername=${MSSQL_USER}" "mssqlPassword=${MSSQL_PASSWORD}" "securityGroupId=${SECURITY_GROUP_ID}" "subnetId=${SUBNET_ID}" \
		--resolve-s3 \
		--stack-name "${STACK_NAME}" \
		--profile crsh-aws \
		--region eu-west-2

