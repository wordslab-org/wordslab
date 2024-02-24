if [ -z "$1" ]; then
    echo "Please provide at least one argument to create a workspace project directory and its associated virtual environment."
    echo "New project in /home/myprojectdir : create-workspace-project myprojectdir"
    echo "Github repo in /home/fastbook     : create-workspace-project https://github.com/fastai/fastbook.git"
    echo "Github repo in /home/myprojectdir : create-workspace-project https://github.com/fastai/fastbook.git myprojectdir"
    exit 1
fi
if [[ $1 == *.git ]]; then
    git_url=$1
    if [ ! -z "$2" ]; then
        dir_name=$2
    else
        dir_name=$(basename $1 .git)
    fi
else
    git_url=""
    dir_name=$1
fi
if [ -d "/home/$dir_name" ]; then
    echo "Directory /home/$dir_name already exists: please choose another project name"
    exit 1
fi
echo "Creating project directory: /home/$dir_name"
mkdir -p /home/$dir_name
cd /home/$dir_name
if [ ! -z "$git_url" ]; then
    echo "Cloning git repository: $git_url"
    git clone $git_url /home/$dir_name
else
    git init 2> /dev/null
fi
echo ".venv" >> .gitignore
git add .gitignore
echo "Creating a virtual environment and Jupyter kernel for project: $dir_name"
python -m venv --system-site-packages --prompt $dir_name .venv
source .venv/bin/activate
python -m ipykernel install --user --name=$dir_name
if [ -f "requirements.txt" ]; then
    echo "Installing the dependencies listed in requirements.txt"
    pip install -r requirements.txt
else
    touch requirements.txt
    git add requirements.txt
fi
if [ -f "setup_environment.sh" ]; then
    echo "Executing the custom script setup_environment.sh"
    . ./setup_environment.sh
else
    touch setup_environment.sh
    git add setup_environment.sh
fi
echo "Virtual environment is ready for project $dir_name"
