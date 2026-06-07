import de.MCmoderSD.objects.MediaProbe;

void main() {

    var mkv = new File("1.mkv");
    var mp4 = new File("1.mp4");

    var mkvProbe = new MediaProbe(mkv);
    var mp4Probe = new MediaProbe(mp4);

    mkvProbe.printInfo();
    IO.println("------------------------------");
    mp4Probe.printInfo();
}