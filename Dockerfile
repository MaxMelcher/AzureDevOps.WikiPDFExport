####################################################
# Azure Devops PDF Exporter Image
# DEV GUIDELINES ###################################
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices
#####################################################

FROM ubuntu:18.04

RUN export DEBIAN_FRONTEND=noninteractive \
    && apt-get -qq -o=Dpkg::Use-Pty=0 update --fix-missing && apt-get -qq -o=Dpkg::Use-Pty=0 install -f -y gconf-service \
    libasound2 \
    libatk1.0-0 \
    libc6 \
    libcairo2\
    libcups2 \
    libdbus-1-3 \
    libexpat1 \
    libfontconfig1 \
    libgcc1 \
    libgconf-2-4 \
    libgdk-pixbuf2.0-0 \
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libstdc++6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    ca-certificates \
    fonts-liberation \
    libappindicator1 \
    libnss3 \
    lsb-release \
    xdg-utils \
    wget \
    libgbm-dev \
    ttf-ancient-fonts\
    # Tidy up
    && apt-get -qq autoremove -y \
    && rm -rf /var/lib/apt/lists/*

RUN export DEBIAN_FRONTEND=noninteractive \
    # Install Microsoft package feed
    && wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    \
    # Install .NET
    && apt-get update \
    && apt-get install -y apt-transport-https \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        dotnet-runtime-6.0 \
    \
    # Cleanup
    && rm -rf /var/lib/apt/lists/*

COPY ./output/linux-x64/azuredevops-export-wiki /usr/local/bin
