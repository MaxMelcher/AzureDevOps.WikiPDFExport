.DEFAULT_GOAL := help

DOCKER_IMG ?= azuredevops-export-wiki:latest
CURRENTTAG:=$(shell git describe --tags --abbrev=0)
NEWTAG ?= $(shell bash -c 'read -p "Please provide a new tag (currnet tag - ${CURRENTTAG}): " newtag; echo $$newtag')

#help: @ List available tasks
help:
	@clear
	@echo "Usage: make COMMAND"
	@echo "Commands :"
	@grep -E '[a-zA-Z\.\-]+:.*?@ .*$$' $(MAKEFILE_LIST)| tr -d '#' | awk 'BEGIN {FS = ":.*?@ "}; {printf "\033[32m%-7s\033[0m - %s\n", $$1, $$2}'

#clean: @ Cleanup
clean:
	@rm -rf ./output

#build: @ Build
build: clean
	cd AzureDevOps.WikiPDFExport && dotnet build azuredevops-export-wiki.csproj && cd ..
	cd AzureDevOps.WikiPDFExport && dotnet publish -r linux-x64 --configuration Release -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:UseAppHost=true -p:Version=4.0.0-beta5 -o output/linux-x64 --no-self-contained && cd ..

#run: @ Run
run: build
	dotnet run --project AzureDevOps.WikiPDFExport/azuredevops-export-wiki.csproj


# upgrade outdated https://github.com/NuGet/Home/issues/4103
#upgrade: @ Upgrade outdated packages
upgrade:
	@cd AzureDevOps.WikiPDFExport && dotnet list package --outdated | grep -o '> \S*' | grep '[^> ]*' -o | xargs --no-run-if-empty -L 1 dotnet add package

#release: @ Create and push a new tag
release: clean
	$(eval NT=$(NEWTAG))
	@echo -n "Are you sure to create and push ${NT} tag? [y/N] " && read ans && [ $${ans:-N} = y ]
	@echo ${NT} > ./version.txt
	@git add -A
	@git commit -a -s -m "Cut ${NT} release"
	@git tag ${NT}
	@git push origin ${NT}
	@git push
	@echo "Done."

#image-build: @ Build Docker image
image-build: build
	docker build --network=host -t ${DOCKER_IMG} -f Dockerfile .

exp-clone:
	git clone git@ssh.dev.azure.com:v3/GRD-GDS/Technology%20Innovation%20Architecture/Technology-Innovation-Architecture.wiki ~/projects/Technology-Innovation-Architecture.wiki

exp-pdf: build
	cd ~/projects/Technology-Innovation-Architecture.wiki/
	cp ~/projects/AzureDevOps.WikiPDFExport-AK/AzureDevOps.WikiPDFExport/output/linux-x64/azuredevops-export-wiki ~/projects/Technology-Innovation-Architecture.wiki/
	cd ~/projects/Technology-Innovation-Architecture.wiki/ && ./azuredevops-export-wiki --disableTelemetry -p TIA-PoC/AKS-EE-PoC/ -o "Guidelines and Best Practices for AKS EE 1.0.pdf" -b  --globaltocposition 0 --globaltoc "AKS-EE-PoC Index"
	xdg-open ~/projects/Technology-Innovation-Architecture.wiki/"Guidelines and Best Practices for AKS EE 1.0.pdf"

yyy:
	cd AzureDevOps.WikiPDFExport &&  dotnet publish -r linux-x64 --configuration Release -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:UseAppHost=true  -p:Version=4.0.0-beta5 -o output/linux-x64 --no-self-contained