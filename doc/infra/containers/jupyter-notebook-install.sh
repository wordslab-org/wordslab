# jupyter/base-notebook:python-3.10.8
# https://github.com/jupyter/docker-stacks/blob/10e52ee84369aaa37630aea13c137b766ace307b/base-notebook/Dockerfile
# 265.43 MB

# FROM jupyter/docker-stacks-foundation

#LABEL maintainer="Jupyter Project <jupyter@googlegroups.com>"

# Fix: https://github.com/hadolint/hadolint/wiki/DL4006
# Fix: https://github.com/koalaman/shellcheck/wiki/SC3014
set -o pipefail

# USER root

# Install all OS dependencies for notebook server that starts but lacks all
# features (e.g., download as all possible file formats)
# - pandoc is used to convert notebooks to html files
#   it's not present in aarch64 ubuntu image, so we install it here
# - run-one - a wrapper script that runs no more
#   than one unique  instance  of  some  command with a unique set of arguments,
#   we use `run-one-constantly` to support `RESTARTABLE` option
apt-get update --yes
apt-get install --yes --no-install-recommends fonts-liberation pandoc run-one
apt-get clean
rm -rf /var/lib/apt/lists/*

# USER ${NB_UID}

# Install Jupyter Notebook, Lab, and Hub
# Generate a notebook server config
# Cleanup temporary files
# Correct permissions
# Do all this in a single RUN command to avoid duplicating all of the
# files across image layers when the permissions change
cd /tmp
mamba install --quiet --yes 'notebook' 'jupyterhub' 'jupyterlab'
jupyter notebook --generate-config
mamba clean --all -f -y
npm cache clean --force
jupyter lab clean
rm -rf "/home/${NB_USER}/.cache/yarn"
fix-permissions "${CONDA_DIR}"
fix-permissions "/home/${NB_USER}"

# EXPOSE 8888

# Copy local files as late as possible to avoid cache busting
wget -qO /usr/local/bin/ https://raw.githubusercontent.com/jupyter/docker-stacks/10e52ee84369aaa37630aea13c137b766ace307b/base-notebook/start-notebook.sh
wget -qO /usr/local/bin/ https://raw.githubusercontent.com/jupyter/docker-stacks/10e52ee84369aaa37630aea13c137b766ace307b/base-notebook/start-singleuser.sh 
# Currently need to have both jupyter_notebook_config and jupyter_server_config to support classic and lab
wget -qO /etc/jupyter/ https://raw.githubusercontent.com/jupyter/docker-stacks/10e52ee84369aaa37630aea13c137b766ace307b/base-notebook/jupyter_server_config.py

# Fix permissions on /etc/jupyter as root
# USER root

# Legacy for Jupyter Notebook Server, see: [#1205](https://github.com/jupyter/docker-stacks/issues/1205)
sed -re "s/c.ServerApp/c.NotebookApp/g" /etc/jupyter/jupyter_server_config.py > /etc/jupyter/jupyter_notebook_config.py
fix-permissions /etc/jupyter/

# HEALTHCHECK documentation: https://docs.docker.com/engine/reference/builder/#healthcheck
# This healtcheck works well for `lab`, `notebook`, `nbclassic`, `server` and `retro` jupyter commands
# https://github.com/jupyter/docker-stacks/issues/915#issuecomment-1068528799
# HEALTHCHECK  --interval=15s --timeout=3s --start-period=5s --retries=3 \
#    CMD wget -O- --no-verbose --tries=1 --no-check-certificate \
#    http${GEN_CERT:+s}://localhost:8888${JUPYTERHUB_SERVICE_PREFIX:-/}api || exit 1

# Switch back to jovyan to avoid accidental container runs as root
# USER ${NB_UID}

# WORKDIR "${HOME}"

# Configure container startup
start-notebook.sh


# jupyter/minimal-notebook:python-3.10.8
# https://github.com/jupyter/docker-stacks/blob/10e52ee84369aaa37630aea13c137b766ace307b/minimal-notebook/Dockerfile
# 434.99 MB

FROM jupyter//base-notebook

# LABEL maintainer="Jupyter Project <jupyter@googlegroups.com>"

# Fix: https://github.com/hadolint/hadolint/wiki/DL4006
# Fix: https://github.com/koalaman/shellcheck/wiki/SC3014
set -o pipefail

# USER root

# Install all OS dependencies for fully functional notebook server
apt-get update --yes*
# Common useful utilities
# git-over-ssh
# less is needed to run help in R
# see: https://github.com/jupyter/docker-stacks/issues/1588
# nbconvert dependencies
# https://nbconvert.readthedocs.io/en/latest/install.html#installing-tex
# Enable clipboard on Linux host systems
apt-get install --yes --no-install-recommends git nano-tiny tzdata unzip vim-tiny openssh-client less texlive-xetex texlive-fonts-recommended texlive-plain-generic xclip
apt-get clean && rm -rf /var/lib/apt/lists/*

# Create alternative for nano -> nano-tiny
update-alternatives --install /usr/bin/nano nano /bin/nano-tiny 10

# Switch back to jovyan to avoid accidental container runs as root
# USER ${NB_UID}

# Add R mimetype option to specify how the plot returns from R to the browser
wget -qO --chown=${NB_UID}:${NB_GID} /opt/conda/lib/R/etc/ https://raw.githubusercontent.com/jupyter/docker-stacks/10e52ee84369aaa37630aea13c137b766ace307b/minimal-notebook/Rprofile.site