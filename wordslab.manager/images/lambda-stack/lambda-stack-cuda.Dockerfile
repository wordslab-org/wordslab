# Lambda Stack from https://lambdalabs.com/
# https://lambdalabs.com/lambda-stack-deep-learning-software
# https://github.com/lambdal/lambda-stack-dockerfiles

FROM ghcr.io/wordslab-org/lambda-stack-server:22.04.2

LABEL org.opencontainers.image.source https://github.com/wordslab-org/wordslab

WORKDIR $HOME

# Add libcuda dummy dependency

# TEMPORARY: as of 09/14/2023, https://github.com/lambdal/lambda-stack-dockerfiles/control was not yet updated to cuda 12.2.
# We need to override the file to avoid lambda-stack-cuda 0.1.14~22.04.1 to trigger the install of libnvidia-compute-535-server
# which would be in conflict the nvidia driver passed through from the host by nvidia-container-runtime.
# Without this fix, you get the following error when trying to launch the container: 
# nvidia-container-cli: mount error: file creation failed: ... /usr/lib/x86_64-linux-gnu/libnvidia-ml.so.1: file exists: unknown
RUN echo -e "\
Package: libcuda1-dummy\n\
Maintainer: Lambda Labs <software@lambdalabs.com>\n\
Version: 12.2\n\
Provides: libcuda1 (= 535)\n\
 , libcuda-5.0-\n\
 , libcuda-5.5-1\n\
 , libcuda-6.0-1\n\
 , libcuda-6.5-1\n\
 , libcuda-7.0-1\n\
 , libcuda-7.5-1\n\
 , libcuda-8.0-1\n\
 , libcuda-9.0-1\n\
 , libcuda-9.1-1\n\
 , libcuda-9.2-1\n\
 , libcuda-10.0-1\n\
 , libcuda-10.1-1\n\
 , libcuda-10.2-1\n\
 , libcuda-11.0-1\n\
 , libcuda-11.1-1\n\
 , libcuda-11.2-1\n\
 , libcuda-11.3-1\n\
 , libcuda-11.4-1\n\
 , libcuda-11.5-1\n\
 , libcuda-11.6-1\n\
 , libcuda-11.7-1\n\
 , libcuda-11.8-1\n\
 , libcuda-12.0-1\n\
 , libcuda-12.1-1\n\
 , libcuda-12.2-1\n\
 , libnvidia-ml1 (= 535)\
" > /var/cache/lambda-stack/control

RUN apt-get update && \
	DEBIAN_FRONTEND=noninteractive apt-get install --yes equivs && \
	equivs-build /var/cache/lambda-stack/control && \
	dpkg -i libcuda1-dummy_12.2_all.deb && \
	rm libcuda1-dummy_12.2* && \
	apt-get remove --yes --purge --autoremove equivs && \
	rm -rf /var/lib/apt/lists/*

# Install Deep learning software stack from Lambda Labs (CUDA)

ENV LAMBDA_STACK_CUDA_VERSION="0.1.14~22.04.1"

RUN apt-get update && \
	DEBIAN_FRONTEND=noninteractive \
		apt-get install \
		--yes \
		--no-install-recommends \
		--option "Acquire::http::No-Cache=true" \
		--option "Acquire::http::Pipeline-Depth=0" \
		lambda-stack-cuda=$LAMBDA_STACK_CUDA_VERSION && \
	rm -rf /var/lib/apt/lists/*

# PATH to the CUDA libraries - necessary for some Python packages
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu/"

# Setup for nvidia-docker
ENV NVIDIA_VISIBLE_DEVICES all
ENV NVIDIA_DRIVER_CAPABILITIES compute,utility
ENV NVIDIA_REQUIRE_CUDA "cuda>=11.8"
