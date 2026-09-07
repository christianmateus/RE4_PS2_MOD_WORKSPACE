namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>Known ETS object names, based on the PS2 object ID reference.</summary>
public static class EtsObjectNames
{
    private static readonly IReadOnlyDictionary<byte, string> Names = new Dictionary<byte, string>
    {
        [0x00]="Janela", [0x01]="Caixa pequena", [0x02]="Caixa grande", [0x03]="Porta",
        [0x04]="Gaveta móvel", [0x05]="Estante", [0x06]="Escada A", [0x07]="Barricada de janela",
        [0x08]="Escada B", [0x09]="Portão de madeira", [0x0A]="Lanterna suspensa", [0x0B]="Lanterna C",
        [0x0D]="Porta metálica A", [0x0E]="Alavanca", [0x0F]="Porta",
        [0x10]="Lanterna A", [0x11]="Barril", [0x12]="Barril explosivo", [0x13]="Porta",
        [0x14]="Tocha", [0x15]="Caixa móvel grande", [0x16]="Porta com trinco", [0x17]="Porta C",
        [0x18]="Porta", [0x19]="Tocha", [0x1A]="Medalhão", [0x1B]="Portão de ferro",
        [0x1D]="Janela ornamentada", [0x1E]="Barril", [0x1F]="Ninho de pássaro",
        [0x20]="Porta D", [0x21]="Porta metálica", [0x22]="Porta", [0x23]="Porta de cela",
        [0x24]="Porta de masmorra", [0x25]="Vidro de estante", [0x26]="Porta metálica", [0x27]="Portão de madeira do castelo",
        [0x29]="Vidro", [0x2A]="Porta E", [0x2B]="Porta F", [0x2C]="Janela",
        [0x2D]="Barril", [0x2E]="Vaso pequeno", [0x2F]="Vaso grande",
        [0x30]="Estátua de cavaleiro", [0x31]="Porta de cela", [0x32]="Porta com insígnia", [0x33]="Porta com maçaneta",
        [0x34]="Porta metálica", [0x35]="Janela", [0x36]="Grade de cela", [0x37]="Grade de cela",
        [0x39]="Porta da prisão", [0x3B]="Porta", [0x3C]="Tanque cilíndrico", [0x3D]="Porta",
        [0x3E]="Porta metálica", [0x3F]="Porta metálica",
        [0x40]="Porta curva de madeira", [0x41]="Porta metálica", [0x42]="Pedaço de madeira", [0x44]="Madeira",
        [0x45]="Porta", [0x46]="Porta com janela redonda", [0x47]="Porta", [0x48]="Vidro",
        [0x49]="Porta", [0x4A]="Janela", [0x4B]="Porta do laboratório", [0x4D]="Porta com maçaneta",
        [0x4E]="Porta com leitor de cartão",
        [0x50]="Janela", [0x51]="Janela de vidro", [0x52]="Cilindro de vidro alto", [0x53]="Objeto cilíndrico alto",
        [0x54]="Janela", [0x56]="Janela", [0x57]="Janela de vidro", [0x58]="Vidro",
        [0x59]="Porta com maçaneta", [0x5A]="Vidro", [0x5B]="Porta quebrada", [0x5C]="Porta quebrada",
        [0x5D]="Janela", [0x5E]="Janela", [0x5F]="Janela",
        [0x60]="Janela", [0x61]="Porta automática", [0x62]="Porta de cela", [0x63]="Porta",
        [0x64]="Vidro", [0x65]="Vidro", [0x66]="Estátua lança-chamas",
        [0x68]="Porta giratória do chefe — direita", [0x69]="Porta giratória do chefe — esquerda"
    };

    public static string Get(byte id) => Names.TryGetValue(id, out string? name) ? name : $"et{id:X2}";
}
