#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""Capture real Win32 EXE, window and Shell shortcut icons to an explicitly supplied local directory."""
import argparse
import ctypes as C
from ctypes import wintypes as W
import json
from pathlib import Path
from PIL import Image


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe',type=Path)
    parser.add_argument('output',type=Path)
    parser.add_argument('--window',type=int)
    parser.add_argument('--shortcut',type=Path)
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    user,gdi,shell=C.windll.user32,C.windll.gdi32,C.windll.shell32
    user.PrivateExtractIconsW.argtypes=[W.LPCWSTR,C.c_int,C.c_int,C.c_int,C.POINTER(W.HICON),C.POINTER(W.UINT),W.UINT,W.UINT]
    user.GetDC.restype=W.HDC
    user.GetDC.argtypes=[W.HWND]
    user.ReleaseDC.argtypes=[W.HWND,W.HDC]
    user.DestroyIcon.argtypes=[W.HICON]
    user.DrawIconEx.argtypes=[W.HDC,C.c_int,C.c_int,W.HICON,C.c_int,C.c_int,W.UINT,W.HBRUSH,W.UINT]
    gdi.CreateCompatibleDC.argtypes=[W.HDC]
    gdi.CreateCompatibleDC.restype=W.HDC
    gdi.CreateDIBSection.argtypes=[W.HDC,C.c_void_p,W.UINT,C.POINTER(C.c_void_p),W.HANDLE,W.DWORD]
    gdi.CreateDIBSection.restype=W.HBITMAP
    gdi.SelectObject.argtypes=[W.HDC,W.HANDLE]
    gdi.SelectObject.restype=W.HANDLE
    gdi.DeleteObject.argtypes=[W.HANDLE]
    gdi.DeleteDC.argtypes=[W.HDC]
    user.SendMessageTimeoutW.argtypes=[W.HWND,W.UINT,W.WPARAM,W.LPARAM,W.UINT,W.UINT,C.POINTER(C.c_size_t)]
    class IconInfo(C.Structure):
        _fields_=[('is_icon',W.BOOL),('x',W.DWORD),('y',W.DWORD),('mask',W.HBITMAP),('color',W.HBITMAP)]
    class Bitmap(C.Structure):
        _fields_=[('type',W.LONG),('width',W.LONG),('height',W.LONG),('stride',W.LONG),('planes',W.WORD),('bits',W.WORD),('pixels',C.c_void_p)]
    user.GetIconInfo.argtypes=[W.HICON,C.POINTER(IconInfo)]
    gdi.GetObjectW.argtypes=[W.HANDLE,C.c_int,C.c_void_p]
    class Header(C.Structure):
        _fields_=[('size',W.DWORD),('width',W.LONG),('height',W.LONG),('planes',W.WORD),('bits',W.WORD),('compression',W.DWORD),('image',W.DWORD),('x',W.LONG),('y',W.LONG),('used',W.DWORD),('important',W.DWORD)]
    def capture(handle,size,name):
        assert handle, 'Missing native icon: '+name
        dc=user.GetDC(None)
        mem=gdi.CreateCompatibleDC(dc)
        pixels=C.c_void_p()
        header=Header(C.sizeof(Header),size,-size,1,32,0,0,0,0,0,0)
        bitmap=gdi.CreateDIBSection(dc,C.byref(header),0,C.byref(pixels),None,0)
        assert bitmap and pixels.value
        previous=gdi.SelectObject(mem,bitmap)
        try:
            C.memset(pixels,0,size*size*4)
            assert user.DrawIconEx(mem,0,0,handle,size,size,0,None,3)
            raw=C.string_at(pixels,size*size*4)
            image=Image.frombytes('RGBA',(size,size),raw,'raw','BGRA')
            # DIB icon colors are premultiplied. Restore straight alpha for the PNG.
            image.putdata([(min(255,round(r*255/a)),min(255,round(g*255/a)),min(255,round(b*255/a)),a) if a else (0,0,0,0) for r,g,b,a in image.get_flattened_data()])
            assert image.getbbox(), 'Empty OS icon: '+name
            image.save(args.output/(name+'.png'))
            return {'name':name,'size':size,'native_handle_present':True,'visible_bounds':image.getbbox()}
        finally:
            gdi.SelectObject(mem,previous)
            gdi.DeleteObject(bitmap)
            gdi.DeleteDC(mem)
            user.ReleaseDC(None,dc)
    records=[]
    for size in (16,20,24,32,40,48,64,128,256):
        icon=W.HICON()
        identifier=W.UINT()
        assert user.PrivateExtractIconsW(str(args.exe.resolve()),0,size,size,C.byref(icon),C.byref(identifier),1,0)==1
        try: records.append(capture(icon,size,f'exe-{size}'))
        finally: user.DestroyIcon(icon)
    if args.window:
        for kind in (0,1):
            icon=C.c_size_t()
            assert user.SendMessageTimeoutW(args.window,0x7f,kind,0,2,1000,C.byref(icon))
            info,bitmap=IconInfo(),Bitmap()
            assert user.GetIconInfo(icon.value,C.byref(info))
            try:
                assert gdi.GetObjectW(info.color,C.sizeof(bitmap),C.byref(bitmap))
                size=bitmap.width
                records.append(capture(icon.value,size,'window-'+('small-' if kind==0 else 'big-')+str(size)))
            finally:
                if info.mask:gdi.DeleteObject(info.mask)
                if info.color:gdi.DeleteObject(info.color)
    if args.shortcut:
        class FileInfo(C.Structure):
            _fields_=[('icon',W.HICON),('index',C.c_int),('attributes',W.DWORD),('display',W.WCHAR*260),('type',W.WCHAR*80)]
        shell.SHGetFileInfoW.argtypes=[W.LPCWSTR,W.DWORD,C.POINTER(FileInfo),W.UINT,W.UINT]
        shell.SHGetFileInfoW.restype=C.c_size_t
        for flags,size in [(0x101,16),(0x100,32)]:
            info=FileInfo()
            assert shell.SHGetFileInfoW(str(args.shortcut.resolve()),0,C.byref(info),C.sizeof(info),flags)
            try: records.append(capture(info.icon,size,'shortcut-'+str(size)))
            finally: user.DestroyIcon(info.icon)
    (args.output/'windows-icons.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(records))


if __name__=='__main__':
    main()
