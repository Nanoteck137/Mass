#include <stdio.h>

typedef struct R {
	int a;
	int b;
} R;

typedef struct T {
	R a;
	int b;
	int c;
} T;

T w = { {1, 2}, 2, 3 };

int main(int argc, char** argv)
{
	T e = { {1, 2}, 2, 3 };
	return 0;
}