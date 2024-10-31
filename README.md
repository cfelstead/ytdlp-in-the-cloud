# YT-DLP In The Cloud

## Introduction
I had a quick need to analyse some videos and produce a word cloud of the audio. Most of this is outside the scope of this particular repo. This repo covers the initial setups, taking the videos URLs, downloading them, storing the videos somewhere else. This application is designed to run in a container so that it can be could hosted. The code uses interfaces so that the data source and save destination can be easily swapped out based on the users desired technology stack. The exact example will use a SQL database for the video URLs and tracking with Azure Blob storage being used to store the final output.
