"""Valida um pacote SGPT1 antes de publicar.

Uso: python validar_patch.py PASTA_DO_PATCH [PASTA_DO_JOGO_ORIGINAL]

Sem a pasta do jogo: confere índice, partes e blocos.
Com a pasta do jogo: também confere os originais e reconstrói cada arquivo
em memória de hash (sem gravar nada), comparando com sha256_resultado.
Sai com código 1 se encontrar qualquer erro.
"""
import hashlib
import json
import os
import sys

BUF = 1 << 22


def sha(path, limite=-1):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        resto = limite
        while resto != 0:
            b = f.read(BUF if resto < 0 else min(BUF, resto))
            if not b:
                break
            h.update(b)
            if resto > 0:
                resto -= len(b)
    return h.hexdigest()


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    pasta, jogo = sys.argv[1], (sys.argv[2] if len(sys.argv) > 2 else None)
    erros = []
    idx = json.load(open(os.path.join(pasta, 'patch_ptbr.json'), encoding='utf-8'))
    print('pacote:', idx.get('pacote'), '| formato:', idx.get('formato'))
    if idx.get('formato') != 'SGPT1':
        erros.append('formato inesperado: %r' % idx.get('formato'))

    partes = idx['partes']
    for p in partes:
        pp = os.path.join(pasta, p['nome'])
        if not os.path.exists(pp):
            erros.append('parte ausente: ' + p['nome'])
            continue
        if os.path.getsize(pp) != p['bytes']:
            erros.append('tamanho errado: %s (%d != %d)' % (p['nome'], os.path.getsize(pp), p['bytes']))
            continue
        print('conferindo', p['nome'], '...', flush=True)
        if sha(pp) != p['sha256']:
            erros.append('hash errado: ' + p['nome'])

    vistos = set()
    for a in idx['arquivos']:
        d = a['destino']
        norm = os.path.normpath(d.replace('/', os.sep))
        if os.path.isabs(norm) or norm.startswith('..'):
            erros.append('destino fora da pasta do jogo: ' + d)
        if norm.lower() in vistos:
            erros.append('destino duplicado: ' + d)
        vistos.add(norm.lower())
        if a['tipo'] not in ('append', 'full', 'novo'):
            erros.append('tipo desconhecido em %s: %s' % (d, a['tipo']))
        base = a.get('bytes_original', 0) if a['tipo'] == 'append' else 0
        soma = 0
        for parte, off, tam in a['blocos']:
            if not 0 <= parte < len(partes):
                erros.append('%s: bloco aponta para parte inexistente %d' % (d, parte))
                continue
            if off < 0 or tam <= 0 or off + tam > partes[parte]['bytes']:
                erros.append('%s: bloco fora da parte %d (%d+%d)' % (d, parte, off, tam))
            soma += tam
        if base + soma != a['bytes_resultado']:
            erros.append('%s: tamanho final não fecha (%d + %d != %d)' % (d, base, soma, a['bytes_resultado']))

    if jogo and not erros:
        abertas = {}
        try:
            for i, a in enumerate(idx['arquivos'], 1):
                d = a['destino']
                alvo = os.path.join(jogo, d.replace('/', os.sep))
                h = hashlib.sha256()
                if a['tipo'] == 'append':
                    if not os.path.exists(alvo) or os.path.getsize(alvo) < a['bytes_original']:
                        erros.append('original ausente ou menor: ' + d)
                        continue
                    with open(alvo, 'rb') as f:
                        resto = a['bytes_original']
                        while resto:
                            b = f.read(min(BUF, resto))
                            h.update(b)
                            resto -= len(b)
                    if h.copy().hexdigest() != a['sha256_original']:
                        erros.append('original diferente do esperado: ' + d)
                        continue
                for parte, off, tam in a['blocos']:
                    f = abertas.setdefault(parte, open(os.path.join(pasta, partes[parte]['nome']), 'rb'))
                    f.seek(off)
                    resto = tam
                    while resto:
                        b = f.read(min(BUF, resto))
                        h.update(b)
                        resto -= len(b)
                if h.hexdigest() != a['sha256_resultado']:
                    erros.append('reconstrução não bate com sha256_resultado: ' + d)
                print('[%d/%d] %s ok' % (i, len(idx['arquivos']), d), flush=True)
        finally:
            for f in abertas.values():
                f.close()

    tot = sum(p['bytes'] for p in partes)
    print('\n%d arquivo(s), %d parte(s), %.2f GB' % (len(idx['arquivos']), len(partes), tot / 1e9))
    if erros:
        print('\nERROS (%d):' % len(erros))
        for e in erros:
            print(' -', e)
        return 1
    print('Tudo certo.' + ('' if jogo else ' (originais não conferidos: passe a pasta do jogo)'))
    return 0


if __name__ == '__main__':
    sys.exit(main())
