linter-local:
	dotnet-format -f CityPCG-unity/Assets/ --exclude Editor

linter-dev:
	#	Run with "--no-cache" if you have edited .editorconfig
	docker build  -f ci/linter-editorconfig/dev.Dockerfile -t linter-editorconfig-dev .
	docker run -v `pwd`:/app linter-editorconfig-dev

linter-prod:
	#	Run with "--no-cache" if you have edited .editorconfig
	docker build  -f ci/linter-editorconfig/prod.Dockerfile -t linter-editorconfig-prod .
	docker run -v `pwd`:/app linter-editorconfig-prod
