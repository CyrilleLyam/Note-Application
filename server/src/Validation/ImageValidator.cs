namespace server.src.Validation;

public enum ImageFormat
{
    Unknown,
    Jpeg,
    Png,
    Gif,
    WebP
}

public static class ImageValidator
{
    private static readonly byte[] WindowsExeSignature = [0x4D, 0x5A]; // "MZ"
    private static readonly byte[] LinuxElfSignature = [0x7F, 0x45, 0x4C, 0x46]; // "\x7fELF"
    private static readonly byte[] MachO32Signature = [0xFE, 0xED, 0xFA, 0xCE];
    private static readonly byte[] MachO64Signature = [0xFE, 0xED, 0xFA, 0xCF];
    private static readonly byte[] MachOFatSignature = [0xCA, 0xFE, 0xBA, 0xBE];

    public static bool IsExecutableOrScript(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 2 && header[0] == WindowsExeSignature[0] && header[1] == WindowsExeSignature[1])
        {
            return true;
        }

        if (header.Length >= 4)
        {
            if (header.StartsWith(LinuxElfSignature) ||
                header.StartsWith(MachO32Signature) ||
                header.StartsWith(MachO64Signature) ||
                header.StartsWith(MachOFatSignature))
            {
                return true;
            }

            // Shell scripts: "#!"
            if (header[0] == 0x23 && header[1] == 0x21)
            {
                return true;
            }

            // PHP scripts: "<?ph"
            if (header[0] == 0x3C && header[1] == 0x3F && (header[2] == 0x70 || header[2] == 0x50) && (header[3] == 0x68 || header[3] == 0x48))
            {
                return true;
            }

            // HTML/XML: "<!DO", "<htm", "<?xm"
            if (header[0] == 0x3C && (header[1] == 0x21 || header[1] == 0x68 || header[1] == 0x48 || header[1] == 0x3F))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryDetectImageFormat(
        ReadOnlySpan<byte> header,
        out ImageFormat format,
        out string contentType,
        out string canonicalExtension)
    {
        format = ImageFormat.Unknown;
        contentType = string.Empty;
        canonicalExtension = string.Empty;

        if (header.Length < 12)
        {
            return false;
        }

        if (IsExecutableOrScript(header))
        {
            return false;
        }

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            format = ImageFormat.Jpeg;
            contentType = "image/jpeg";
            canonicalExtension = ".jpg";
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header.Length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            format = ImageFormat.Png;
            contentType = "image/png";
            canonicalExtension = ".png";
            return true;
        }

        // GIF: "GIF87a" (47 49 46 38 37 61) or "GIF89a" (47 49 46 38 39 61)
        if (header.Length >= 6 &&
            header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38 &&
            (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61)
        {
            format = ImageFormat.Gif;
            contentType = "image/gif";
            canonicalExtension = ".gif";
            return true;
        }

        // WEBP: "RIFF" (bytes 0-3) and "WEBP" (bytes 8-11)
        if (header.Length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            format = ImageFormat.WebP;
            contentType = "image/webp";
            canonicalExtension = ".webp";
            return true;
        }

        return false;
    }
}
