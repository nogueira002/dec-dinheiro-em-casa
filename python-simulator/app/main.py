from flask import Flask, jsonify

app = Flask(__name__)

@app.route('/health-check', methods=['GET'])
def health_check():
    return jsonify({
        "status": "ok",
        "service": "python-simulator",
        "version": "1.0.0"
    }), 200

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)