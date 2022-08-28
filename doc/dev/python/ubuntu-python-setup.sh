#!/bin/bash

# --- EXECUTE WITH SUDO ---

# wsl Ubuntu 20.04

# https://github.com/jupyter/docker-stacks/blob/main/base-notebook/Dockerfile

export NB_USER="laurent"
export NB_GID="1000"

# Pin python version here, or set it to "default"
PYTHON_VERSION=3.10

export DEBIAN_FRONTEND=noninteractive
apt-get update --yes && apt-get upgrade --yes
apt-get install --yes --no-install-recommends bzip2 ca-certificates fonts-liberation locales pandoc run-one sudo wget
apt-get clean && rm -rf /var/lib/apt/lists/*
echo "en_US.UTF-8 UTF-8" > /etc/locale.gen && locale-gen

export CONDA_DIR=/opt/conda
mkdir -p "${CONDA_DIR}"
export PATH="${CONDA_DIR}/bin:${PATH}"
cat << EOF >> "/home/${NB_USER}/.bashrc"

export PATH="${CONDA_DIR}/bin:\$PATH"
EOF

# Download and install Micromamba, and initialize Conda prefix.
#   <https://github.com/mamba-org/mamba#micromamba>
#   Similar projects using Micromamba:
#     - Micromamba-Docker: <https://github.com/mamba-org/micromamba-docker>
#     - repo2docker: <https://github.com/jupyterhub/repo2docker>
# Install Python, Mamba, Jupyter Notebook, Lab, and Hub
# Generate a notebook server config
# Cleanup temporary files and remove Micromamba
# Correct permissions
cat << EOF >> "${CONDA_DIR}/.condarc"
auto_update_conda: false
show_channel_urls: true
channels:
  - conda-forge
EOF
cd /tmp
set -x && \
    arch=$(uname -m) && \
    if [ "${arch}" = "x86_64" ]; then \
        # Should be simpler, see <https://github.com/mamba-org/mamba/issues/1437>
        arch="64"; \
    fi && \
    wget -qO /tmp/micromamba.tar.bz2 \
        "https://micromamba.snakepit.net/api/micromamba/linux-${arch}/latest" && \
    tar -xvjf /tmp/micromamba.tar.bz2 --strip-components=1 bin/micromamba && \
    rm /tmp/micromamba.tar.bz2 && \
    PYTHON_SPECIFIER="python=${PYTHON_VERSION}" && \
    if [[ "${PYTHON_VERSION}" == "default" ]]; then PYTHON_SPECIFIER="python"; fi && \
    # Install the packages
    ./micromamba install \
        --root-prefix="${CONDA_DIR}" \
        --prefix="${CONDA_DIR}" \
        --yes \
        "${PYTHON_SPECIFIER}" \
        'mamba' \
        'notebook' \
        'jupyterhub' \
        'jupyterlab' && \
    rm micromamba && \
    # Pin major.minor version of python
    mamba list python | grep '^python ' | tr -s ' ' | cut -d ' ' -f 1,2 >> "${CONDA_DIR}/conda-meta/pinned" && \
    jupyter notebook --generate-config && \
    mamba clean --all -f -y && \
    npm cache clean --force && \
    jupyter lab clean && \
    rm -rf "/home/${NB_USER}/.cache/yarn"

# Currently need to have both jupyter_notebook_config and jupyter_server_config to support classic and lab
mkdir /etc/jupyter/
cat << EOF > /etc/jupyter/jupyter_server_config.py
import os
import stat
import subprocess

from jupyter_core.paths import jupyter_data_dir

c = get_config()  # noqa: F821
c.ServerApp.ip = "0.0.0.0"
c.ServerApp.port = 8888
c.ServerApp.open_browser = False

# to output both image/svg+xml and application/pdf plot formats in the notebook file
c.InlineBackend.figure_formats = {"png", "jpeg", "svg", "pdf"}

# https://github.com/jupyter/notebook/issues/3130
c.FileContentsManager.delete_to_trash = False

# Generate a self-signed certificate
OPENSSL_CONFIG = """\
[req]
distinguished_name = req_distinguished_name
[req_distinguished_name]
"""
if "GEN_CERT" in os.environ:
    dir_name = jupyter_data_dir()
    pem_file = os.path.join(dir_name, "notebook.pem")
    os.makedirs(dir_name, exist_ok=True)

    # Generate an openssl.cnf file to set the distinguished name
    cnf_file = os.path.join(os.getenv("CONDA_DIR", "/usr/lib"), "ssl", "openssl.cnf")
    if not os.path.isfile(cnf_file):
        with open(cnf_file, "w") as fh:
            fh.write(OPENSSL_CONFIG)

    # Generate a certificate if one doesn't exist on disk
    subprocess.check_call(
        [
            "openssl",
            "req",
            "-new",
            "-newkey=rsa:2048",
            "-days=365",
            "-nodes",
            "-x509",
            "-subj=/C=XX/ST=XX/L=XX/O=generated/CN=generated",
            f"-keyout={pem_file}",
            f"-out={pem_file}",
        ]
    )
    # Restrict access to the file
    os.chmod(pem_file, stat.S_IRUSR | stat.S_IWUSR)
    c.ServerApp.certfile = pem_file

# Change default umask for all subprocesses of the notebook server if set in
# the environment
if "NB_UMASK" in os.environ:
    os.umask(int(os.environ["NB_UMASK"], 8))
EOF

# Legacy for Jupyter Notebook Server, see: [#1205](https://github.com/jupyter/docker-stacks/issues/1205)
sed -re "s/c.ServerApp/c.NotebookApp/g" \
    /etc/jupyter/jupyter_server_config.py > /etc/jupyter/jupyter_notebook_config.py 

# Install all OS dependencies for fully functional notebook server
apt-get install --yes --no-install-recommends git tzdata unzip vim-tiny openssh-client less texlive-xetex texlive-fonts-recommended texlive-plain-generic 
apt-get clean && rm -rf /var/lib/apt/lists/*

# Fix permissions

fix-permissions () {
    set -e
    for d in "$@"; do
        find "${d}" \
            ! \( \
                -group "${NB_GID}" \
                -a -perm -g+rwX \
            \) \
            -exec chgrp "${NB_GID}" {} \; \
            -exec chmod g+rwX {} \;
        # setuid, setgid *on directories only*
        find "${d}" \
            \( \
                -type d \
                -a ! -perm -6000 \
            \) \
            -exec chmod +6000 {} \;
    done
}

fix-permissions "${CONDA_DIR}"
fix-permissions /etc/jupyter/