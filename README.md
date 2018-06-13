# ipfs-uploader

Files manipulation and adds to IPFS gracefully

Confirmed working on Windows 7+ and Linux. Untested on mac.

Dependencies required:
* either `dotnet-sdk-2.0` for dev
* or `dotnet-runtime` for production
* `ffmpeg`
* `ffprobe`
* `imagemagick`
* `ipfs` (go-ipfs) with a running deamon

## Starting it in dev mode
Navigate to the `Uploader.Web` directory and just do `dotnet run`

## Available Calls
Once your instance is started, it should listen on localhost:5000 by default and serve all these calls.
## GET
* `/getStatus` and `/getStatus?details=true` will give some information about the running instance such as queues times and usage statistics
* `/getErrors` will give information about recent errors
* `/index.html` allows you to easily test video, image and subtitles uploads. Only available in dev mode
* `/getProgressByToken/:token` answers with a json object representing the progress of the upload
* * token: is the token given after a succesful `/uploadVideo` or `/uploadImage`
* `/getProgressBySourceHash/:token` same as above but takes the source video hash as input

## POST
* `/uploadVideo` The main video upload call
  * POST file input `files`. Needs to be a video otherwise it will end up as an error.
* * Query String Options
    * videoEncodingFormats: controls which video resolutions are requested for the encoding
    * sprite: true/false. Controls whether a sprite file should be generated for this video.

Curl example: ``curl -F "video=@./video.mp4"  http://localhost:5000/uploadVideo?videoEncodingFormats=240p,480p,720p&sprite=true``

* `/uploadImage` 
  * POST file input `files`. Needs to be an image otherwise it will end up as an error.

* `/uploadSubtitle` 
  * POST text input `subtitle`. Needs to be a string starting with WEBVTT otherwise it will end up as an error.

## Configuration
The main config file is available as `appsettings.json`.

* Front :
  * "CORS": "https://d.tube"					Cross-origin domains to authorize requests from.
* General :
  * "MaxGetProgressCanceled": 20,				The number of seconds after which an upload gets cancelled if the user doesn't request a `/getProgressByX`.
  * "ImageMagickPath": "",						The path to imagemagick (leave blank if you use linux).
  * "TempFilePath": "/tmp/dtube",		Path of temporary files created during the encoding and ipfs processes.
  * "ErrorFilePath": "/home/dtube/errors",	Path where uploaded files creating errors get stored .
  * "FinalFilePath": "/home/dtube/success",	Path where succesfully uploaded files to IPFS get stored (if OnlyHash option is true).
  * "Version": "0.7.5"							The version of the running instance.
* Encode (array) : the list of possible video qualities encoded by this instance.
  * "urlTag": "720p",							The tag that represents a quality.
  * "maxRate": "2000k",						The video bitrate.
  * "width": 1280,							Video width.
  * "height": 720,							Video height.
  * "MinSourceHeightForEncoding": 600,		Minimum source height for allowing encoding to this quality.
  * "qualityOrder": 5							To order different qualities. Higher is higher quality.
* Ipfs :
  * "IpfsTimeout": 108000,						IPFS add timeout in seconds
  * "VideoAndSpriteTrickleDag": true,			If true, uses -t (trickle dag) option for adding video and sprite files to IPFS. Recommended.
  * "AddVideoSource": true,						If true, the source file also gets added to IPFS.
  * "OnlyHash": false							If true, uses --only-hash option of IPFS and moves files to the FinalFilePath instead or adding to the ipfs datastore directly.
* Video :
  * "FfProbeTimeout": 10,						ffprobe timeout in seconds.
  * "EncodeGetImagesTimeout": 600,				Timeout in seconds for generating all pictures composing the sprite.
  * "EncodeTimeout": 108000,					Timeout in seconds for video encoding.
  * "MaxVideoDurationForEncoding": 1800,		Maximum video duration in seconds for video encoding.
  * "NbSpriteImages": 100,						Number of total images used in the sprite.
  * "HeightSpriteImages": 118,					Sprite height.
  * "GpuEncodeMode": true,						Enables GPU-encoding.
  * "NbSpriteDaemon": 0,						Number of daemons generating sprites.
  * "NbAudioVideoCpuEncodeDaemon": 0,			Number of daemons for CPU audio and video encoding.
  * "NbAudioCpuEncodeDaemon": 0,				Number of daemons for CPU audio only encoding (used when doing GPU video encoding).
  * "NbVideoGpuEncodeDaemon": 0,				Number of daemons for GPU video encoding.
  * "AuthorizedQuality": "240p,480p,720p",				Authorized encoding qualities. Any other requested quality will be ignored.
  * "NVidiaCard":"QuadroP5000"					GPU card model.

## Building and running it in production

### Building

`dotnet publish -c Release` will create a /bin/Release/netcoreapp2.0/publish folder

### Running

Navigate to the publish folder and `dotnet Uploader.Web.dll`

### Logging

Everything gets logged inside the `logs` folder and each process has it's own log file (ffmpeg, ipfs, etc). If you want logs in a different folder, you need to edit the `log4net.config` file.
