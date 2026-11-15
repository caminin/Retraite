# Assemble les captures de docs/captures en une seule image légendée.
# Usage : python3 tools/vm/scripts/assembler-apercu.py docs/captures
import sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

dossier = sys.argv[1]
F = "/usr/share/fonts/truetype/noto/"
def police(taille, gras=False):
    return ImageFont.truetype(F + ("NotoSans-Bold.ttf" if gras else "NotoSans-Regular.ttf"), taille)

FOND = (245, 246, 248); ENCRE = (22, 32, 46); DISCRET = (93, 107, 126); ACCENT = (11, 107, 203)

panneaux = [
    ("1-bureau.png", None, "Sur le bureau",
     "Le nombre de jours travaillés restants s'affiche en bas à droite, par-dessus le fond d'écran, qui reste celui de votre choix. Les fenêtres ouvertes passent toujours devant, et le chiffre se met à jour chaque jour tout seul."),
    ("2-reglages.png", (168, 18, 1112, 728), "Les réglages",
     "On choisit le premier jour de retraite, puis on clique dans le calendrier sur les jours de vacances (en bleu). Le compte se met à jour aussitôt."),
    ("4-confirmation.png", (168, 18, 1112, 728), "Un jour chaque semaine, l'apparence",
     "« Je ne travaille pas le vendredi » s'applique d'un clic, après une confirmation claire. En bas : la position, la taille et la couleur du compteur."),
]

MARGE, ECART = 70, 60
L = 2400
LARG_HERO = L - 2 * MARGE
LARG_CASE = (L - 2 * MARGE - ECART) // 2

def preparer(nom, recadrage, largeur):
    im = Image.open(f"{dossier}/{nom}").convert("RGB")
    if recadrage: im = im.crop(recadrage)
    return im.resize((largeur, round(im.height * largeur / im.width)), Image.LANCZOS)

def lignes(texte, fnt, largeur, d):
    mots, res, cur = texte.split(), [], ""
    for m in mots:
        essai = (cur + " " + m).strip()
        if d.textlength(essai, font=fnt) <= largeur: cur = essai
        else: res.append(cur); cur = m
    res.append(cur); return res

f_titre, f_sous, f_case, f_texte, f_num = police(64, True), police(30), police(36, True), police(28), police(30, True)
tmp = ImageDraw.Draw(Image.new("RGB", (10, 10)))

hero = preparer(*panneaux[0][:2], LARG_HERO)
cases = [preparer(n, r, LARG_CASE) for n, r, _, _ in panneaux[1:]]
def haut_legende(texte, largeur): return 60 + len(lignes(texte, f_texte, largeur - 70, tmp)) * 40
H_HERO = hero.height + 30 + haut_legende(panneaux[0][3], LARG_HERO)
H_CASES = max(c.height for c in cases) + 30 + max(haut_legende(t, LARG_CASE) for *_, t in panneaux[1:])
ENTETE = 200
H = ENTETE + H_HERO + ECART + H_CASES + MARGE

toile = Image.new("RGB", (L, H), FOND)
d = ImageDraw.Draw(toile)
d.text((MARGE, 55), "Compteur retraite", font=f_titre, fill=ENCRE)
d.text((MARGE, 135), "Aperçu de l'application, capturé sur Windows 11", font=f_sous, fill=DISCRET)

def poser(im, x, y, numero, titre, texte, largeur, haut_img):
    ombre = Image.new("RGBA", (im.width + 40, im.height + 40), (0, 0, 0, 0))
    ImageDraw.Draw(ombre).rounded_rectangle((20, 24, im.width + 20, im.height + 24), 14, fill=(0, 0, 0, 60))
    ombre = ombre.filter(ImageFilter.GaussianBlur(12))
    toile.paste(ombre, (x - 20, y - 20), ombre)
    masque = Image.new("L", im.size, 0)
    ImageDraw.Draw(masque).rounded_rectangle((0, 0, im.width, im.height), 14, fill=255)
    toile.paste(im, (x, y), masque)
    ty = y + haut_img + 30
    d.ellipse((x, ty + 2, x + 48, ty + 50), fill=ACCENT)
    num = str(numero)
    d.text((x + 24 - d.textlength(num, font=f_num) / 2, ty + 5), num, font=f_num, fill="white")
    d.text((x + 66, ty + 2), titre, font=f_case, fill=ENCRE)
    for k, l in enumerate(lignes(texte, f_texte, largeur - 70, d)):
        d.text((x + 66, ty + 56 + k * 40), l, font=f_texte, fill=DISCRET)

poser(hero, MARGE, ENTETE, 1, panneaux[0][2], panneaux[0][3], LARG_HERO, hero.height)
y2 = ENTETE + H_HERO + ECART
hmax = max(c.height for c in cases)
for i, (c, (_, _, titre, texte)) in enumerate(zip(cases, panneaux[1:])):
    poser(c, MARGE + i * (LARG_CASE + ECART), y2, i + 2, titre, texte, LARG_CASE, hmax)

sortie = f"{dossier}/apercu-compteur-retraite.png"
toile.save(sortie, optimize=True)
print(sortie, toile.size)
