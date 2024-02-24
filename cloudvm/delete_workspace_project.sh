if [ -z "$1" ]; then
    echo "Please provide the name of the workspace project directory you want to delete."
    echo "Delete project in /home/myprojectdir : delete-workspace-project myprojectdir"
    exit 1
else
    dir_name=$1
fi
if [ ! -d "/home/$dir_name" ]; then
    echo "Directory /home/$dir_name not found: please choose another project name"
    exit 1
fi
echo "Deleting the Jupyter kernel for project: $dir_name"
jupyter kernelspec uninstall -y $dir_name
echo "Deleting the workspace project directory: /home/$dir_name"
rm -rf /home/$dir_name
