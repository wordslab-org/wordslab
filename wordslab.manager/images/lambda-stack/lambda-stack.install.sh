### wsl --install Ubuntu-22.04 ###

# Install prerequisites

sudo -- bash -c 'apt-get update && apt-get install --yes git wget && git clone https://github.com/lambdal/lambda-stack-dockerfiles.git /var/cache/lambda-stack && rm -rf /var/lib/apt/lists/*'

# Setup Lambda repository

sudo -- bash -c 'apt-get update && apt-get install --yes gnupg && gpg --dearmor -o /etc/apt/trusted.gpg.d/lambda.gpg < /var/cache/lambda-stack/lambda.gpg && echo "deb http://archive.lambdalabs.com/ubuntu jammy main" > /etc/apt/sources.list.d/lambda.list && echo "Package: *" > /etc/apt/preferences.d/lambda && echo "Pin: origin archive.lambdalabs.com" >> /etc/apt/preferences.d/lambda && echo "Pin-Priority: 1001" >> /etc/apt/preferences.d/lambda && echo "cudnn cudnn/license_preseed select ACCEPT" | debconf-set-selections && rm -rf /var/lib/apt/lists/*'

# Install Lambda Server system

sudo -- bash -c 'export LAMBDA_SERVER_VERSION="22.04.2" && apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends --option "Acquire::http::No-Cache=true" --option "Acquire::http::Pipeline-Depth=0" lambda-server=$LAMBDA_SERVER_VERSION && rm -rf /var/lib/apt/lists/*'

# Add libcuda dummy dependency

sudo -- bash -c 'apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install --yes equivs && equivs-build /var/cache/lambda-stack/control && dpkg -i libcuda1-dummy_11.8_all.deb && rm libcuda1-dummy_11.8* && apt-get remove --yes --purge --autoremove equivs && rm -rf /var/lib/apt/lists/*'

# Install Deep learning software stack from Lambda Labs (CUDA)

sudo -- bash -c 'export LAMBDA_STACK_CUDA_VERSION="0.1.14~22.04.1" && apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends --option "Acquire::http::No-Cache=true" --option "Acquire::http::Pipeline-Depth=0" lambda-stack-cuda=$LAMBDA_STACK_CUDA_VERSION && rm -rf /var/lib/apt/lists/*'

# PATH to the CUDA libraries - necessary for some Python packages
export LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu/"

# Setup for nvidia-docker
export NVIDIA_VISIBLE_DEVICES all
export NVIDIA_DRIVER_CAPABILITIES compute,utility
export NVIDIA_REQUIRE_CUDA "cuda>=11.8"

# libnvidia-compute-535-server
# nvidia-cuda-toolkit (12.2.128)
# python3 (3.10.12)
# python3-numpy (1.21.5)
# python3-sklearn-lib (0.23.2)
# python3-torch-cuda (2.0.1)
# python3-pandas-lib (1.3.5)
# python3-jaxlib-cuda (0.4.13)
# python3-tensorflow-cuda (2.13.0)
# python3-keras (2.13.1)