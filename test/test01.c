#include <stdio.h>

int a[4];
int b = 1;
int *c = 0x00000001;

typedef struct Wooh
{
	int a;
	int b;
} Wooh;

Wooh l = {.b = 1};

int main(int argc, char **argv)
{
	Wooh woh;
	puts("Wooh");
	return 0;
}