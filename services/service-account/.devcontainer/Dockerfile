FROM mcr.microsoft.com/devcontainers/anaconda:1-3

# Copy environment.yml and requirements.txt
COPY ../environment.yml* /tmp/conda-tmp/
COPY ../requirements.txt* /tmp/pip-tmp/

RUN if [ -f "/tmp/conda-tmp/environment.yml" ]; then umask 0002 && /opt/conda/bin/conda env update -n base -f /tmp/conda-tmp/environment.yml; fi \
    && rm -rf /tmp/conda-tmp

RUN if [ -f "/tmp/pip-tmp/requirements.txt" ]; then pip install -r /tmp/pip-tmp/requirements.txt; fi \
    && rm -rf /tmp/pip-tmp

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>
