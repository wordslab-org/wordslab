# Jupyter stacks images

https://jupyter-docker-stacks.readthedocs.io/en/latest/using/selecting.html

jupyter/docker-stacks-foundation (125 MB compressed)
- mamba
- unpriviledged user jovyan, /home/joyvan and /opt/conda
- tini init as the container entrypoint
- options for a passwordless sudo

jupyter/base-notebook (266 MB compressed)
- minimally-functional Jupyter Notebook server (e.g., no LaTeX support for saving notebooks as PDFs)
- notebook, jupyterhub and jupyterlab packages
- a start-notebook.sh script as the default command
- options for a self-signed HTTPS certificate

jupyter/minimal-notebook (435 MB compressed)
- TeX Live for notebook document conversion
- git, vi (actually vim-tiny), nano (actually nano-tiny), tzdata, and unzip

Notes:
- default user 'jovyan': https://docs.jupyter.org/en/latest/community/content-community.html#what-is-a-jovyan
- tini init system: https://computingforgeeks.com/use-tini-init-system-in-docker-containers/
- TeX Live: https://www.tug.org/texlive/
- nano: https://www.geeksforgeeks.org/nano-vs-vim-editor-whats-the-difference-between-nano-and-vim-editors/
- tzdata: https://packages.debian.org/en/sid/tzdata

https://jupyter-docker-stacks.readthedocs.io/en/latest/using/common.html

- you can pass Jupyter server options to the start-notebook.sh script when launching the container
- ex: "start-notebook.sh --NotebookApp.base_url=/customized/url/prefix/"
- Jupyter Server options: https://jupyter-server.readthedocs.io/en/latest/operators/public-server.html

- -e NB_USER=<username> - The desired username and associated home folder
- ex:--user root -e NB_USER="my-username" -e CHOWN_HOME=yes -w "/home/${NB_USER}"

- e GRANT_SUDO=yes - Instructs the startup script to grant the NB_USER user passwordless sudo capability
- You do not need this option to allow the user to conda or pip install additional packages. 
- This option is helpful for cases when you wish to give ${NB_USER} the ability to install OS packages with apt or modify other root-owned files in the container

- -e DOCKER_STACKS_JUPYTER_CMD=<jupyter command> - Instructs the startup script to run jupyter ${DOCKER_STACKS_JUPYTER_CMD} instead of the default jupyter lab command
- -e RESTARTABLE=yes - Runs Jupyter in a loop so that quitting Jupyter does not cause the container to exit. 
- This may be useful when installing extensions that require restarting Jupyter.
- -e NOTEBOOK_ARGS="--log-level='DEBUG' --dev-mode" - Adds custom options to add to jupyter commands. 
- This way, the user could use any option supported by jupyter subcommand.

- -v /some/host/folder/for/work:/home/jovyan/work - Mounts a host machine directory as a folder in the container. 
- This configuration is useful for preserving notebooks and other work even after the container is destroyed.
- You must grant the within-container notebook user or group (NB_UID or NB_GID) write access to the host directory (e.g., sudo chown 1000 /some/host/folder/for/work).

- You may mount an SSL key and certificate file into a container and configure the Jupyter Server to use them to accept HTTPS connections
- start-notebook.sh --NotebookApp.keyfile=/etc/ssl/notebook/notebook.key --NotebookApp.certfile=/etc/ssl/notebook/notebook.crt

# Conda/Mamba environments

The default Python 3.x Conda environment resides in /opt/conda. T
The /opt/conda/bin directory is part of the default jovyan user’s ${PATH}. 
The jovyan user has full read/write access to the /opt/conda directory. 
You can use either mamba, pip or conda (mamba is recommended) to install new packages without any additional permissions.

> mamba install --quiet --yes some-package && \
>    mamba clean --all -f -y && \
>    fix-permissions "${CONDA_DIR}" && \
>    fix-permissions "/home/${NB_USER}"

> mamba install --channel defaults humanize

https://docs.conda.io/projects/conda/en/latest/user-guide/concepts/environments.html

Conda directory structure
- ROOT_DIR (/opt/conda)
- /pkgs - This directory contains decompressed packages, ready to be linked in conda environments. Each package resides in a subdirectory corresponding to its canonical name.
- /envs - The system location for additional conda environments to be created.

The following subdirectories comprise a conda environment:
- /bin
- /include
- /lib
- /share

A virtual environment is a tool that helps to keep dependencies required by different projects separate by creating isolated spaces for them that contain per-project dependencies for them.

https://towardsdatascience.com/a-guide-to-conda-environments-bc6180fc533

conda can both
- Install packages (written in any language) from repositories like Anaconda Repository and Anaconda Cloud.
- Install packages from PyPI by using pip in an active Conda environment.

> conda create -n conda-env python=3.7
> conda activate conda-env 

The easiest way to make your work reproducible by others is to include a file in your project’s root directory listing all the packages, along with their version numbers, that are installed in your project’s environment.
Conda calls these environment files. 

> conda env export --file environment.yml  
> conda env create -n conda-env -f /path/to/environment.yml

Because conda leverages hardlinks, it is easy to overestimate the space really being used, especially if one only looks at the size of a single env at a time.
First, if I count each environment directory individually, I get the uncorrected per env usage
$ for d in envs/*; do du -sh $d; done
2.4G    envs/pymc36
1.7G    envs/pymc3_27
1.4G    envs/r-keras
1.7G    envs/stan
1.2G    envs/velocyto

Most of the hardlinks go back to the pkgs directory, so if we include that as well:
$ du -sh pkgs envs/*
8.2G    pkgs
400M    envs/pymc36
116M    envs/pymc3_27
 92M    envs/r-keras
 62M    envs/stan
162M    envs/velocyto
one can see that outside of the shared packages, the envs are fairly light. 
If you're concerned about the size of my pkgs, note that I have never run conda clean on this system, so my pkgs directory is full of tarballs and superseded packages, plus some infrastructure I keep in base (e.g., Jupyter, Git, etc).

# Helm chart for Jupyter stacks images

https://github.com/MaastrichtU-IDS/dsri-helm-charts/tree/main/charts/jupyterlab/

## 

# Traefik ingress config

https://doc.traefik.io/traefik/providers/kubernetes-crd/

# GPU Jupyter Notebooks

https://github.com/iot-salzburg/gpu-jupyter

https://github.com/anibali/docker-pytorch/raw/master/dockerfiles/1.13.0-cuda11.8-ubuntu22.04/Dockerfile

https://towardsdatascience.com/a-complete-guide-to-building-a-docker-image-serving-a-machine-learning-system-in-production-d8b5b0533bde