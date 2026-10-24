#ifndef _PORTAL_H_
#define _PORTAL_H_

#define PORTAL_MAX_FDS 1

typedef struct Portal Portal;

Portal* Portal_Get(void);

int Portal_Fds(Portal* portal, int* fds, int max);
int Portal_Pump(Portal* portal);

int Portal_ReducedMotion(Portal* portal);
int Portal_HighContrast(Portal* portal);
int Portal_TextScale(Portal* portal);

#endif
