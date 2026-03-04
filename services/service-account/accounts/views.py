from django.http import JsonResponse # type: ignore

def health(request):
    return JsonResponse({'status': 'ok'})

def root(request):
    return JsonResponse({'message': 'Welcome to the Account Service microservice'})