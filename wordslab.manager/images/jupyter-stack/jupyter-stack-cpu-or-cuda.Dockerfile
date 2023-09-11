# Jupyter Docker Stacks from https://jupyter-docker-stacks.readthedocs.io/
# https://jupyter-docker-stacks.readthedocs.io/en/latest/using/selecting.html
# https://github.com/jupyter/docker-stacks/

# Build this image twice:
# --build-arg TARGET="cpu"
# --build-arg TARGET="cuda"
ARG TARGET=cpu
ARG BASE_IMAGE_VERSION=0.1.14-22.04.1
ARG BASE_IMAGE=ghcr.io/wordslab-org/lambda-stack-$TARGET:$BASE_IMAGE_VERSION

FROM $BASE_IMAGE

LABEL org.opencontainers.image.source https://github.com/wordslab-org/wordslab

RUN git clone https://github.com/jupyter/docker-stacks.git /var/cache/docker-stacks

# ------------------------------------------------------------
# https://github.com/jupyter/docker-stacks/tree/main/images/docker-stacks-foundation

# Fix: https://github.com/hadolint/hadolint/wiki/DL4006
# Fix: https://github.com/koalaman/shellcheck/wiki/SC3014
SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# Install all OS dependencies for notebook server that starts but lacks all
# features (e.g., download as all possible file formats)
RUN apt-get update --yes && \
    DEBIAN_FRONTEND=noninteractive \
    apt-get install --yes --no-install-recommends \
    locales \
    # - tini is installed as a helpful container entrypoint that reaps zombie
    #   processes and such of the actual executable we want to start, see
    #   https://github.com/krallin/tini#why-tini for details.
    tini \
    wget \
    # - Python virtual environments will be needed to manage several projects
    # inside a single development workspace
    python3.10-venv && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Configure locale
RUN echo "en_US.UTF-8 UTF-8" > /etc/locale.gen && \
    locale-gen

ENV LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    LANGUAGE=en_US.UTF-8

# Enable prompt color
RUN sed -i 's/^#force_color_prompt=yes/force_color_prompt=yes/' ${HOME}/.bashrc

# Create workspace directory
# IMPORTANT: you should MOUNT A PERSISTENT STORAGE volume here
RUN mkdir -p /workspace

# ------------------------------------------------------------
# https://github.com/jupyter/docker-stacks/blob/main/images/base-notebook/Dockerfile

# Install all OS dependencies for notebook server that starts but lacks all
# features (e.g., download as all possible file formats)
RUN apt-get update --yes && \
    DEBIAN_FRONTEND=noninteractive \
    apt-get install --yes --no-install-recommends \
    fonts-liberation \
    # - pandoc is used to convert notebooks to html files
    #   it's not present in aarch64 ubuntu image, so we install it here
    pandoc \
    # - run-one - a wrapper script that runs no more
    #   than one unique  instance  of  some  command with a unique set of arguments,
    #   we use `run-one-constantly` to support `RESTARTABLE` option
    run-one && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# JupyterLab source extensions require Node.js to rebuild JupyterLab and activate the extension
ENV NODE_MAJOR=18
RUN apt-get update --yes && \
    apt-get install -y ca-certificates curl gnupg && \
    mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
RUN echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list && \
    apt-get update --yes && \
    apt-get install nodejs -y && \
    rm -rf /var/lib/apt/lists/*

# Install Jupyter Notebook, Lab, and Hub
# Generate a notebook server config
# Cleanup temporary files
# Correct permissions
# Do all this in a single RUN command to avoid duplicating all of the
# files across image layers when the permissions change
RUN pip install \
    'notebook' \
    'jupyterhub' \
    'jupyterlab==3.6.5' \
    'ipywidgets' && \
    jupyter notebook --generate-config && \
    npm cache clean --force && \
    jupyter lab clean && \
    rm -rf "${HOME}/.cache/yarn"

# TODO: we need to pin jupyterlab to version 3.6.5 above because jupyterlab-git doesn't support v4 yet
# https://github.com/jupyterlab/jupyterlab-git/issues/1245

# JUPYTER SERVER startup parameters can be customized through environment variables
# - Jupyter URL:  http://127.0.0.1:${JUPYTER_PORT}${JUPYTER_BASE_URL}/lab?token=${JUPYTER_TOKEN}
ENV JUPYTER_BASE_URL="/"
ENV JUPYTER_PORT=8888
ENV JUPYTER_TOKEN=""

# Currently need to have both jupyter_notebook_config and jupyter_server_config to support classic and lab
RUN mkdir -p /etc/jupyter && \
    cp /var/cache/docker-stacks/base-notebook/jupyter_server_config.py /etc/jupyter/ && \
    # Allow root execution and remote access in a Kubernetes setup
    # https://jupyter-server.readthedocs.io/en/stable/other/full-config.html
    echo -e '\
c.ServerApp.allow_root = True\n\
c.ServerApp.allow_remote_access = True\n\
c.ServerApp.disable_check_xsrf = True\n\
c.ServerApp.allow_origin = "*"\n\
c.ServerApp.trust_xheaders = True\n\
c.ServerApp.port_retries = 0\n\
\n\
c.ZMQChannelsWebsocketConnection.iopub_msg_rate_limit = 100000000\n\
c.ZMQChannelsWebsocketConnection.iopub_data_rate_limit = 2147483647\n\
c.MappingKernelManager.buffer_offline_messages = True\n\
\n\
c.Application.log_level = "WARN"\n\
c.ServerApp.log_level = "WARN"\n\
c.JupyterApp.answer_yes = True\n\
\n\
c.PasswordIdentityProvider.allow_password_change = False\n\
c.ServerApp.quit_button = False\n\
c.FileContentsManager.delete_to_trash = False\n\
c.IPKernelApp.matplotlib = "inline"\n\
\n\
WORKSPACE_HOME = "/workspace"\n\
try:\n\
    if os.path.exists(WORKSPACE_HOME + "/templates"):\n\
        c.JupyterLabTemplates.template_dirs = [WORKSPACE_HOME + "/templates"]\n\
    c.JupyterLabTemplates.include_default = False\n\
except Exception:\n\
    pass\n\
' >> /etc/jupyter/jupyter_server_config.py && \
    # Legacy for Jupyter Notebook Server, see: [#1205](https://github.com/jupyter/docker-stacks/issues/1205)
    cp /etc/jupyter/jupyter_server_config.py /etc/jupyter/jupyter_notebook_config.py 

# HEALTHCHECK documentation: https://docs.docker.com/engine/reference/builder/#healthcheck
# This healtcheck works well for `lab`, `notebook`, `nbclassic`, `server` and `retro` jupyter commands
# https://github.com/jupyter/docker-stacks/issues/915#issuecomment-1068528799
HEALTHCHECK  --interval=5s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -O- --no-verbose --tries=1 --no-check-certificate \
    http://127.0.0.1:${JUPYTER_PORT}${JUPYTER_BASE_URL}/api || exit 1

# ------------------------------------------------------------
# https://github.com/jupyter/docker-stacks/blob/main/images/minimal-notebook/Dockerfile

# Install all OS dependencies for fully functional notebook server
RUN apt-get update --yes && \
    apt-get install --yes --no-install-recommends \
    # Common useful utilities
    nano-tiny \
    unzip \
    # git-over-ssh
    openssh-client \
    # less is needed to run help in R
    # see: https://github.com/jupyter/docker-stacks/issues/1588
    less \
    # nbconvert dependencies
    # https://nbconvert.readthedocs.io/en/latest/install.html#installing-tex
    texlive-xetex \
    texlive-fonts-recommended \
    texlive-plain-generic \
    # Enable clipboard on Linux host systems
    xclip && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Create alternative for nano -> nano-tiny
RUN update-alternatives --install /usr/bin/nano nano /bin/nano-tiny 10

# Add R mimetype option to specify how the plot returns from R to the browser
RUN mkdir -p ${HOME}/.local/lib/R/etc && \
    cp /var/cache/docker-stacks/minimal-notebook/Rprofile.site ${HOME}/.local/lib/R/etc/

# ------------------------------------------------------------
# Install useful JupyterLab extensions
# https://saturncloud.io/blog/top-33-jupyterlab-extensions-2023/

# [Notebook templates]
# JupyterLab templates: https://github.com/finos/jupyterlab_templates
RUN pip install jupyterlab_templates && \
    jupyter labextension install jupyterlab_templates && \
    jupyter server extension enable --py jupyterlab_templates && \
    npm cache clean --force && jupyter lab clean && rm -rf "${HOME}/.cache/yarn"

# [Notebook code editor]
# Language Server Protocol: https://github.com/jupyter-lsp/jupyterlab-lsp
# RUN pip install jupyterlab-lsp 'python-lsp-server[all]'
# TODO: try again when enabling jupterlab v4

# [Notebook versioning]
# Git: https://github.com/jupyterlab/jupyterlab-git
RUN pip install jupyterlab-git && \
# Jupyter Notebook Diff and Merge tools: https://github.com/jupyter/nbdime
    pip install nbdime

# [Notebook perf monitoring]
# Execution time: https://github.com/deshaw/jupyterlab-execute-time
RUN pip install jupyterlab_execute_time && \
 # NVDashboard: https://github.com/rapidsai/jupyterlab-nvdashboard
    pip install jupyterlab-nvdashboard

# [Notebook visualisation]
# Matplotlib: https://github.com/matplotlib/ipympl
RUN pip install ipympl && \
# TensorBoard: https://pypi.org/project/jupyterlab-tensorboard-pro/
    pip install jupyterlab-tensorboard-pro

# ------------------------------------------------------------
# Container scripts for initialization and startup commands

# Use bash as the default shell in JupyterLab
ENV SHELL="/bin/bash"

# JUPYTER WORKSPACE files and directories can be customized through environment variables.
# All the path values are absolute paths under the /workspace mount point.
# - Jupyter file explorer root directory: /workspace${JUPYTER_ROOT_DIR}
ENV JUPYTER_ROOT_DIR="/"
# - Optional script launched at container startup to install additional system packages: /workspace${JUPYTER_CONTAINER_SETUP_SCRIPT}
ENV JUPYTER_SETUP_SCRIPT="/setup.sh"

# - Make sur the Jupyter config is stored in the persistent volume
ENV JUPYTER_DATA_DIR="/workspace/.jupyter"
ENV JUPYTER_RUNTIME_DIR="/workspace/.jupyter/runtime"
# https://jupyterlab.readthedocs.io/en/stable/user/directories.html
ENV JUPYTERLAB_SETTINGS_DIR="/workspace/.jupyter/lab/user-settings/"
ENV JUPYTERLAB_WORKSPACES_DIR="/workspace/.jupyter/lab/workspaces"

# Workspace start script
RUN echo -e '\
# Optional script launched at container startup to install additional system packages\n\
SETUP_FILE="/workspace${JUPYTER_SETUP_SCRIPT}" test -x $SETUP_FILE && $SETUP_FILE\n\
# JUPYTER SERVER startup parameters can be customized through environment variables\n\
jupyter lab -ServerApp.base_url="${JUPYTER_BASE_URL}" -ServerApp.port=${JUPYTER_PORT} -IdentityProvider.token="${JUPYTER_TOKEN}" -ServerApp.root_dir="/workspace${JUPYTER_ROOT_DIR}"\n\
' > /usr/local/bin/start-workspace.sh && chmod u+x /usr/local/bin/start-workspace.sh

# Create Python virtual environments for specific projects
RUN echo -e '\
if [ -z "$1" ]; then\n\
    echo "Please provide at least one argument to create a workspace project directory and its associated virtual environment."\n\
    echo "New project in /workspace/myprojectdir : create-workspace-project myprojectdir"\n\
    echo "Github repo in /workspace/fastbook     : create-workspace-project https://github.com/fastai/fastbook.git"\n\
    echo "Github repo in /workspace/myprojectdir : create-workspace-project https://github.com/fastai/fastbook.git myprojectdir"\n\
    exit 1\n\
fi\n\
if [[ $1 == *.git ]]; then\n\
    git_url=$1\n\
    if [ ! -z "$2" ]; then\n\
        dir_name=$2\n\
    else\n\
        dir_name=$(basename $1 .git)\n\
    fi\n\
else\n\
    git_url=""\n\
    dir_name=$1\n\
fi\n\
if [ -d "/workspace/$dir_name" ]; \n\
    echo "Directory /workspace/$dir_name already exists: please choose another project name"\n\
    exit 1\n\
fi\n\
echo "Creating project directory: /workspace/$dir_name"\n\
mkdir -p /workspace/$dir_name\n\
cd /workspace/$dir_name\n\
if [ ! -z "$git_url" ]; then\n\
    echo "Cloning git repository: $git_url"\n\
    git clone $git_url /workspace/$dir_name\n\
else\n\
    git init 2> /dev/null\n\
fi\n\
echo ".venv" >> .gitignore\n\
git add .gitignore\n\
echo "Creating a virtual environment and Jupyter kernel for project: $dir_name"\n\
python -m venv --system-site-packages --prompt $dir_name .venv\n\
source .venv/bin/activate\n\
python -m ipykernel install --user --name=$dir_name\n\
if [ -f "requirements.txt" ]; then\n\
    echo "Installing the dependencies listed in requirements.txt"\n\
    pip install -r requirements.txt\n\
else\n\
    touch requirements.txt\n\
    git add requirements.txt\n\
fi\n\
if [ -f "setup_environment.sh" ]; then\n\
    echo "Executing the custom script setup_environment.sh"\n\
    . ./setup_environment.sh\n\
else\n\
    touch setup_environment.sh\n\
    git add setup_environment.sh\n\
fi\n\
echo "Virtual environment is ready for project $dir_name"\n\
' > /usr/local/bin/create-workspace-project && chmod u+x /usr/local/bin/create-workspace-project && \
echo -e '\
if [ -z "$1" ]; then\n\
    echo "Please provide the name of the workspace project directory you want to delete."\n\
    echo "Delete project in /workspace/myprojectdir : delete-workspace-project myprojectdir"\n\
    exit 1\n\
else\n\
    dir_name=$1\n\
fi\n\
if [ ! -d "/workspace/$dir_name" ]; then\n\
    echo "Directory /workspace/$dir_name not found: please choose another project name"\n\
    exit 1\n\
fi\n\
echo "Deleting the Jupyter kernel for project: $dir_name"\n\
jupyter kernelspec uninstall $dir_name\n\
echo "Deleting the workspace project directory: /workspace/$dir_name"\n\
rm -rf /workspace/$dir_name\n\
' > /usr/local/bin/delete-workspace-project && chmod u+x /usr/local/bin/delete-workspace-project

# Configure container startup
ENTRYPOINT ["tini", "-g", "--"]
CMD ["start-workspace.sh"]
