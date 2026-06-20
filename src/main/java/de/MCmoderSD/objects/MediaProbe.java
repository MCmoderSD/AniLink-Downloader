package de.MCmoderSD.objects;

import org.bytedeco.ffmpeg.global.avutil;
import org.bytedeco.javacv.FFmpegFrameGrabber;
import org.bytedeco.javacv.FrameGrabber;

import java.io.File;
import java.util.concurrent.TimeUnit;

import static java.util.concurrent.TimeUnit.MILLISECONDS;

@SuppressWarnings("unused")
public class MediaProbe {

    // Attributes
    private final File mediaFile;
    private final int width;
    private final int height;
    private final String codec;
    private final double framerate;
    private final double bitrate;
    private final double gamma;
    private final long duration;
    private final long frames;

    // Constructor
    public MediaProbe(File mediaFile) {

        // Set media file
        this.mediaFile = mediaFile;

        // Set Log Level
        avutil.av_log_set_level(avutil.AV_LOG_QUIET);

        // Initialize FFmpegFrameGrabber
        try (var grabber = new FFmpegFrameGrabber(mediaFile)) {

            // Lock File
            grabber.start();

            // Extract Media Information
            width = grabber.getImageWidth();
            height = grabber.getImageHeight();
            codec = grabber.getVideoCodecName();
            framerate = grabber.getVideoFrameRate();
            bitrate = grabber.getVideoBitrate();
            gamma = grabber.getGamma();
            duration = grabber.getLengthInTime();
            frames = grabber.getLengthInFrames();

            // Release File
            grabber.stop();

        } catch (FrameGrabber.Exception e) {
            throw new RuntimeException("Failed to probe media file: " + mediaFile.getAbsolutePath(), e);
        }
    }

    // Methods
    public void printInfo() {
        IO.println("Media Information:");
        IO.println("File: " + getMediaFile().getAbsolutePath());
        IO.println("Width: " + getWidth() + "px");
        IO.println("Height: " + getHeight() + "px");
        IO.println("Resolution: " + getResolution());
        IO.println("Codec: " + getCodec());
        IO.println("Framerate: " + getFramerate());
        IO.println("Bitrate: " + getBitrate());
        IO.println("Gamma: " + getGamma());
        IO.println("Duration: " + getDuration(MILLISECONDS) + "ms");
        IO.println("Frames: " + getFrames());
    }

    // Getters
    public File getMediaFile() {
        return mediaFile;
    }

    public int getWidth() {
        return width;
    }

    public int getHeight() {
        return height;
    }

    public String getResolution() {
        return width + "x" + height;
    }

    public String getCodec() {
        return codec;
    }

    public double getFramerate() {
        return framerate;
    }

    public double getBitrate() {
        return bitrate;
    }

    public double getGamma() {
        return gamma;
    }

    public long getDuration() {
        return duration;
    }

    public long getDuration(TimeUnit timeUnit) {
        return timeUnit.convert(duration, TimeUnit.MICROSECONDS);
    }

    public long getFrames() {
        return frames;
    }
}